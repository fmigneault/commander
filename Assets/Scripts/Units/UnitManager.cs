﻿// Display debugging/logging info on console
//#define OUTPUT_DEBUG

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using AI;

namespace Units
{
	public class UnitManager : MonoBehaviour 
	{                
		// General parameters
		public string Name = "";
		public string Code = "";
		public Color FactionColor = Color.white;
		public bool IsGroundUnit = false;
		public bool IsAirUnit = false;      

		// Attack parameters
		public bool CanAttackGround = false;
		public bool CanAttackAir = false;
		public float MinAttackRange = 0;
		public float MaxAttackRange = 0;

        // Health and hit points
        public int Health = 100;                    // Current health of the unit
        public int HitPoints = 10;                  // Dammage inflicted by attacks
        private int maxHealth;                      // Equal to current health at initialization (memory)

        // Unit production parameters
        public float ProductionDelay = 0; 

        // Movement parameters
        public float MovingSpeed = 0;
        public float RotationSpeed = 0;

        // Delays for unit destuction 
        public float DestructionMinimumDelay = 5;   // Minimum (in case the destruction effect is not specified)
        public float AfterDestructionDelay = 4;     // Delay to wait after the effects stop emitting

        // Speed at which the unit will translate by the offset value through the ground until disappearing
        public float DestructionTranlateSpeed = 0.01f;
        public float DestructionTranlateOffset = 4;

		// Selected unit highlight on ground reference
		public GameObject SelectionSprite = null;

        // Icon used to represent the unit on the Mini-Map
        public SpriteRenderer MiniMapIconSprite = null;

        // Combinations of particle systems used to simulate the destruction or movement of the unit
        public GameObject DestroyExplosionEffect = null;
        public GameObject MovementEffect = null;

        // Parameters for building construction (only if "builder" unit)
		public List<GameObject> ProducedBuildings = null;

        // Internal memory of requested destination, the waypoint path and the index along these waypoints
        //    Important: Temporarily assign origin position, but will be updated on initialization (Awake)
        private Vector3 destinationRequest = Vector3.zero;
        private Vector3[] waypointPath = null;  // Current waypoints defining the path to follow
        private int waypointIndex = 0;          // Current waypoint position along the path

        // Pathfinding parameters to limit excessive requests
        private const float delayBetweenRequests = 0.25f;       // Elapsed time required for the next path request
        private float elapsedTimeSinceLastRequest = 0f;         // Tracking of time since last pathfinding request
        private const float minimumDistanceForRequest = 2f;     // Minimum target movement required for path update

        // Internal memory of attack target
        private GameObject attackTarget = null; // Currently assigned target the unit was commanded to attack    
        private bool newAttackCommand = false;  // Indicates if the currently assigned target has just been assigned

        // To allow displaying effects on multiple subsequent movemnets, we require more than one effect instance
        //    Using only one instance sometimes makes it  suddenly, because following movement require the 
        //    effect faster than it can complete it's previous call. Therefore, we use a list of available effects
        //    that we gradually cycle through upon each new movement.
        private const int totalMovementEffects = 5;         // Quantity of ParticleSystem effects to instanciate
        private int activeMovementEffect = 0;               // Control variable to cycle through the effects as neede
        private bool previousMovement = false;              // Control variable to switch to the next effect
        private List<GameObject> movementEffects = null;    // List of instanciated ParticleSystems

        // Value that multiplies the color of every part of the unit when it gets destroyed
        private Color destroyColorMultiplier = new Color(0.9f, 0.9f, 0.9f);       


		void Awake()
		{
            // Initialize components and visual effects hidden
			InitializeSelectionHighlight();
            InitializeMiniMapIcon();
            InitializeParticleEffects();
            HealthBar = GetComponentInChildren<HealthBarManager>();
            maxHealth = Health;
            UnitID = UnitTracker.GetNewUnitID();

            // Minimum degree angle required to skip the unit rotation, above will require rotate toward destination
            PermissiveDestinationAngleDelta = 1;

            // If the unit is already in the scene when launching the game, setting the destination to the current 
            // position of the GameObject here has the same result as setting it at the variable declaration above.
            // But, if a new unit instance is requested while the game is running, the reference of the unit to be
            // instanciated will immediately exist while the actual instance will only be generated on the next frame.
            // Therefore, 'MoveToDestination' could be called to set the desired destination, but it would immediately 
            // be overriden by the first 'Start' call when it is instanciated on the next frame.
            // Setting the variable in the 'Start' call resolves the problem no matter when the unit is instanciated.
            if (destinationRequest.Equals(Vector3.zero)) destinationRequest = transform.position;           
		}


        void Start()
        {
            // Update position on 'Start' to ensure the grid has been generated beforehand in its 'Awake' call
            UpdatePositionOnGrid();
        }


        void Update()
        {    
            // Update timer
            elapsedTimeSinceLastRequest += Time.deltaTime;

            // Update health, then apply actions accordingly to specified commands and current statuses
            if (HealthBar != null) HealthBar.Health = (float)Health / (float)maxHealth;
            if (Health > 0)
            {            
                CheckNextWaypointInPath();    
                UpdateAttackUnit();
                if (!UpdateRotateUnit()) return;
                UpdateMoveUnit();
            }
            else
            {
                StartCoroutine(DestroyUnit());
            }
        }


        // Angle offset allowed to skip unit rotation to align itself toward its destination
        public float PermissiveDestinationAngleDelta { get; private set; }


        // Reference to an attached HealthBar object (searched automatically at initialization)
        public HealthBarManager HealthBar { get; private set; }


        // Function for outside calls to request new destinations
        public void MoveToDestination(Vector3 destination, bool overridePathfinding = false)
        {      
            // Stop attacking if it was, then request to move
            AttackTarget(null);

            // Request a new pathfinding search to get path waypoints or specify a linear movement accordingly
            if (overridePathfinding)
            {
                destinationRequest = destination;
                waypointPath = null;
            }
            else
            {
                ControlledPathRequest(transform.position, destination);
            }
        }


        // Function for outside calls to request new target to attack
        public void AttackTarget(GameObject target)
        {
            // Do nothing if requested unit to attack is itself    
            if (target == gameObject) return;

            // Indicate that the attack command is for a new target if different than the last one assigned
            //    This help limiting the pathfinding requests to avoid redundant calls for a same unit 
            if (target != attackTarget) newAttackCommand = true;

            // Update the assigned target
            attackTarget = target;
        }


        private void UpdateMoveUnit()
        {     
            // Calculate the new intermediate position toward the destination and update accordingly
            var newPosition = Vector3.MoveTowards(transform.position, destinationRequest, MovingSpeed * Time.deltaTime);
            if (newPosition != transform.position)
            {
                previousMovement = true;            // Indicate the unit is in movement
                transform.position = newPosition;   // Adjust the intermediate new position toward the destination

                // Update the unit's position on the grid to avoid collisions with other units (unwalkable by others)
                UpdatePositionOnGrid();

                // Move, rotate and display the movement effects at the current unit location
                if (movementEffects != null)
                {
                    var activeEffect = movementEffects[activeMovementEffect];
                    activeEffect.transform.position = newPosition; 
                    activeEffect.transform.rotation = transform.rotation;
                    StartCoroutine(EffectManager.LoopParticleSystems(activeEffect));
                }
            }
            else
            {
                // Stop emitting new particles for reached destination, already emitted ones will gradually disappear
                if (movementEffects != null)
                {
                    var activeEffect = movementEffects[activeMovementEffect];
                    StartCoroutine(EffectManager.StopParticleSystemsCompleteAnimation(activeEffect));
                }
            }
        }


        private bool UpdateRotateUnit()
        {     
            // Calculate the new intermediate rotation toward the destination and update accordingly
            var towardDestination = destinationRequest - transform.position;
            var angleFromDestination = Vector3.Angle(towardDestination, transform.forward);
            if (angleFromDestination > PermissiveDestinationAngleDelta && !towardDestination.Equals(Vector3.zero))
            {   
                // Update the unit's position on the grid to avoid collisions with other units (unwalkable by others)
                UpdatePositionOnGrid();

                // Stop the particle emission if rotation is needed, otherwise, it makes a weird visible effect where 
                // the particles suddenly rotate when the next emission is requested as the forward movement resumes
                if (movementEffects != null)
                {
                    var activeEffect = movementEffects[activeMovementEffect];
                    StartCoroutine(EffectManager.StopParticleSystemsCompleteAnimation(activeEffect));
                }

                // Update the control variables for following frames that could require particle emission with movement
                if (previousMovement) activeMovementEffect = ++activeMovementEffect % totalMovementEffects;
                previousMovement = false;

                // Apply the intermediate rotation toward the target destination
                var rotationDestination = Quaternion.LookRotation(towardDestination, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDestination,  
                                                              RotationSpeed * Time.deltaTime);
                return false;
            }
            return true;    // Finished rotation (already approximately pointing toward target)
        }


        private void UpdateAttackUnit()
        {            
            bool inSight = true;
            bool tooCloseToTarget = false;
            bool inRange = false;
            if (attackTarget != null)
            {
                var targetUnitManager = attackTarget.GetComponent<UnitManager>();
                if (targetUnitManager != null)
                {
                    if (targetUnitManager.Health <= 0)
                    {
                        attackTarget = null;        // Stop attacking the now destroyed target

                        // If the target gets destroyed, in some cases like when the backtrack position is calculated 
                        // because of too close range, the unit tends to still move at the target's position because the 
                        // waypoints path still exists and the next position gets extracted automatically. Also, in 
                        // some cases, a previous request has not yet finished being processed, and when it does, it 
                        // overrides the adjustments done here. Therefore, adjust the unit's destination to its current 
                        // location, reset the waypoints to stop moving and indicate a controlled cancel path request.
                        destinationRequest = transform.position;
                        waypointPath = null;        // Reset waypoints path       
                        CancelPathRequest = true;   // Cncel any next path request received after waypoint reset
                    }
                    else if (RespectsAttackTypes(targetUnitManager))
                    {
                        // Get current status of unit and its target
                        var targetPosition = attackTarget.transform.position;
                        bool targetChangedPosition = false;
                        var distanceFromTarget = Vector3.Distance(transform.position, targetPosition);
                        inRange = InAttackRange(targetPosition);
                        tooCloseToTarget = distanceFromTarget < MinAttackRange;
                        if (waypointPath != null && waypointPath.Length > 0)
                        {
                            // Target is in sight if the unit is on the last waypoint path segment, which ensures 
                            // automatically that it avoids shooting through obstacles because of how pathfinding works
                            // In the case that the unit is too close, the in-sight status must instead be checked with
                            // the first waypoint segment because the requested waypoints path is to move away, which 
                            // makes it possible to move at final location located behind an obstacle while the unit 
                            // and its target are currently not obstructed on the first segment. 
                            inSight = tooCloseToTarget ? (waypointIndex == 0) : 
                                                         (waypointPath.Length - waypointIndex) <= 1;

                            // Checking an area instead of simply verifying for different positions slightly limits the
                            // quantity of pathfinding requested without affecting too much the destination precision.
                            //    The target will have to move sufficiently away to trigger a new calculation instead 
                            //    of extensively calling new requests on each frame as target position are continuously 
                            //    changing as displacements are occuring.
                            var lastKnownTargetPosition = waypointPath[waypointPath.Length - 1];
                            targetChangedPosition = !InRangeArea(targetPosition, lastKnownTargetPosition);
                        }

                        // If already within attack range for a new target to attack, cancel any movement
                        //    This allows attacking right away the target without moving the unit
                        if (newAttackCommand && inRange)
                        {
                            destinationRequest = transform.position;
                            newAttackCommand = false;   // Reset for next calls
                        }

                        // If the unit just got in-range but wasn't previously (ie: was moving and reached the target),
                        // return a midway position between the minimum and maximum attack range to produce realistic
                        // approach of the unit toward its target. 
                        //    Since stopping immediately at the position corresponding to the maximum distance as the
                        //    unit justs get within range would require another movement as soon a the target moves 
                        //    slightly away, this small position adjustment makes the movements look smoother.
                        else if (inSight && inRange && !targetChangedPosition)
                        {
                            destinationRequest = GetPositionInRange(targetPosition);
                        }
                            
                        // If too close to the target, move away until the minimum range is respected, but stay close 
                        // to ensure that the target will be in range for longer if it tries to escape afterwards
                        else if (tooCloseToTarget)
                        {
                            var backtrackPosition = GetPositionTooClose(targetPosition);
                            ControlledPathRequest(transform.position, backtrackPosition);
                        }

                        // Request a new pathfinding search to get path waypoints only as necessary
                        //    - A new request is needed if the assigned target has changed (new target specified)
                        //    - If the  target has sufficiently moved away from its previously known location and
                        //      that it was previously directly in sight, the new position might make it out of 
                        //      sight by an obstacle, so validation is required.
                        else if (newAttackCommand || targetChangedPosition)
                        {
                            ControlledPathRequest(transform.position, targetPosition);
                            newAttackCommand = false;   // Reset for next calls
                        }                            
                    }
                    // Reject any set target if it doesn't match any previous condition
                    else attackTarget = null;
                }
            }

            // Attack movement and shooting animation when there is a target in-range and in-sight
            //  - If out-of-sight, cancel any attack command temporarily and stop shooting (null target)
            //  - If the unit is out-of-range, but still in-sight, keep aiming but hold-fire
            //  - If the unit is out-of-range and out-of-sight, stop aiming at the target
            AttackDelegate(inSight ? attackTarget : null, !inRange, inRange);
        }


        public int UnitID { get; private set; }


		private bool RespectsAttackTypes(UnitManager targetUnitManager)
		{
			return ((targetUnitManager.IsAirUnit && CanAttackAir) || 
                    (targetUnitManager.IsGroundUnit && CanAttackGround));
		}


        private static bool InRangeArea(Vector3 targetPosition, Vector3 lastKnownPosition)
        {
            // Check an approximate area around the last known position
            return Vector3.Distance(targetPosition, lastKnownPosition) <= minimumDistanceForRequest;
        }


        public bool InAttackRange(Vector3 targetPosition) 
		{       
            double distance = Vector3.Distance(targetPosition, transform.position);
            return (distance >= MinAttackRange && distance <= MaxAttackRange);
		}


        private Vector3 GetPositionInRange(Vector3 targetPosition)
        {            
            float distance = Vector3.Distance(transform.position, targetPosition);
            float midRange = (MaxAttackRange - MinAttackRange) / 2;
            return Vector3.Lerp(targetPosition, transform.position, (MaxAttackRange - midRange) / distance);
        }


        private Vector3 GetPositionTooClose(Vector3 targetPosition)
        {
            return transform.position + (transform.position - targetPosition).normalized * MinAttackRange;
        }


		// Function that delegates "Attack" calls to sub-classes of the GameObject linked to this "UnitManager" class
        private void AttackDelegate(GameObject target, bool holdFire, bool aimBarrel)
		{
            if (gameObject.tag == "Tank")
            {
                var tank = GetComponent<TankManager>();
                tank.HoldFire = holdFire;
                tank.AimBarrel = aimBarrel;
                tank.AimingTarget = target;
            }
		}


        private void UpdatePositionOnGrid()
        {            
            // Find the BoxCollider's minimum and maximum boundaries to 
            // specify as a localized region to search for obstructions
            var corners = new Vector3[2];
            var colliderBounds = GetComponent<BoxCollider>().bounds;

            // Extend the bounds to make sure to capture the unit's current, previous and next positions to 
            // properly update the grid object values for all surrounding nodes affected the unit at any time
            colliderBounds.extents = new Vector3(colliderBounds.extents.x * 2, 0, colliderBounds.extents.z * 2);
            corners[0] = colliderBounds.min;                   
            corners[1] = colliderBounds.max;

            // Request the update of grid obstruction by the unit, or reset the area when the unit is destroyed
            GridRequestManager.RequestGridAreaUpdate(corners, UnitID, Health <= 0);
        }       


        public IEnumerator DestroyUnit()
        {            
            // Stop the unit onto it's current location (cannot move anymore) and stop attacking any previous target
            destinationRequest = transform.position;
            attackTarget = null;

            // Find all sub-parts of the unit to have their color values adjusted
            var unitParts = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var part in unitParts)
            {
                foreach (var mat in part.materials)
                {
                    if (mat.HasProperty("_Color")) mat.color *= destroyColorMultiplier;
                }
            }

            // Display the destruction effect and wait for it to complete before destroying the unit (or minimum delay)
            if (DestroyExplosionEffect != null)
            {     
                var delay = Mathf.Max(DestructionMinimumDelay, EffectManager.GetMaximumDuration(DestroyExplosionEffect));
                DestroyExplosionEffect.transform.position = transform.position;
                StartCoroutine(EffectManager.LoopParticleSystems(DestroyExplosionEffect));
                yield return new WaitForSeconds(delay);
                StartCoroutine(EffectManager.StopParticleSystemsCompleteAnimation(DestroyExplosionEffect));
            }
            yield return new WaitForSeconds(AfterDestructionDelay);

            // Translate the unit through the terrain to make it disappear
            var destroyTranslatePosition = transform.position;
            destroyTranslatePosition.y -= DestructionTranlateOffset;
            while (transform.position != destroyTranslatePosition)
            {                
                transform.position = Vector3.MoveTowards(transform.position, destroyTranslatePosition, 
                                                         DestructionTranlateSpeed * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            // Reset the grid area to make it available to other units
            UpdatePositionOnGrid();

            // Destroy all instanciated references, then destoy the actual unit instance
            DestroyInstanciatedReferences();
            Destroy(gameObject);
        }


        public bool SelectionHighlightState
        {
            get { return SelectionSprite != null && SelectionSprite.activeSelf; }
            set { if (SelectionSprite != null) SelectionSprite.SetActive(value); }
        }


		private void InitializeSelectionHighlight()
		{
            SpriteRenderer selectHighlightSprite = null;
            if (SelectionSprite != null) selectHighlightSprite = SelectionSprite.GetComponent<SpriteRenderer>();
            if (selectHighlightSprite != null) selectHighlightSprite.color = FactionColor;
            SelectionHighlightState = false;
		}


        private void InitializeMiniMapIcon()
        {
            if (MiniMapIconSprite != null) MiniMapIconSprite.color = FactionColor;
        }


        private void InitializeParticleEffects()
        {
            // Initialize particle effects to be used with 'EffectManager', or set to null if not possible
            DestroyExplosionEffect = EffectManager.InitializeParticleSystems(DestroyExplosionEffect);
            if (MovementEffect != null)
            {
                movementEffects = new List<GameObject>(totalMovementEffects);
                for (int i = 0; i < totalMovementEffects; i++)
                {
                    movementEffects.Add(EffectManager.InitializeParticleSystems(MovementEffect));
                }
            }
        }


        private void DestroyInstanciatedReferences()
        {
            foreach (var effect in movementEffects) if (effect != null) Destroy(effect);
            if (DestroyExplosionEffect != null) Destroy(DestroyExplosionEffect);
            if (tag == "Tank") GetComponent<TankManager>().DestroyInstanciatedReferences();
        }


        // One-Shot flag that will cancel updating the next received path request
        // Used to account for delays between a request, its reception, and new events conditiosn occuring in between
        // Used in conjunction with the controlled path request function
        private bool CancelPathRequest { get; set; }


        // Controlled path request call using the interlock one-shot flag 
        // This function should always be used instead of accessing directly the 'PathRequestManager'
        private void ControlledPathRequest(Vector3 startPosition, Vector3 endPosition)
        {            
            if (elapsedTimeSinceLastRequest >= delayBetweenRequests)
            {
                CancelPathRequest = false;          // Reset flag since the next call is intentional for a new path

                // Send a new pathfinding request using the unit and target IDs for grid collision validation
                var attackTargetID = attackTarget != null ? attackTarget.GetComponent<UnitManager>().UnitID 
                                                          : UnitTracker.InvalidID;
                PathRequestManager.RequestPath(startPosition, endPosition, OnPathFound, UnitID, attackTargetID);

                elapsedTimeSinceLastRequest = 0;    // Reset timer for future pathfinding requests limitation
            }
        }
            

        // Callback method for when the path request has finished being processed 
        public void OnPathFound(Vector3[] newPath, bool pathSuccess)
        {
            // Update only if pathfinding was successful and not cancelled while the trajectory was being processed
            if (pathSuccess && !CancelPathRequest)
            {           
                waypointPath = newPath;                 // Update the found path waypoints
                waypointIndex = 0;                      // Start at the first waypoint position as a destination
                destinationRequest = waypointPath[0];   // Initial waypoint in path as first destination                
            }
            else if (CancelPathRequest)
            {
                CancelPathRequest = false;  // Reset one-shot since the processed path was received and cancelled
            }
        }


        // Assigns the next waypoint in the path as a new destination when the current one is reached by the unit
        private void CheckNextWaypointInPath() 
        {    
            // If the is no path specified return immediately
            if (waypointPath == null) return;

            // If the last waypoint was reached, the unit is at the final destination. Unset the path waypoints.
            if (waypointIndex >= waypointPath.Length) waypointPath = null;
            // If the currently set waypoint destination is reached, pass to the next waypoint in the path
            else if (waypointPath[waypointIndex] == transform.position) waypointIndex++;
            // Otherwise, adjust the next waypoints as the current unit's required destination
            else destinationRequest = waypointPath[waypointIndex];
        }


        // Draws the waypoints with connected lines to form the returned path to avoid obstables
        private void OnDrawGizmos()
        {
            if (waypointPath != null) 
            {
                for (int i = waypointIndex; i < waypointPath.Length; i++) 
                {                    
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(waypointPath[i], Vector3.one);

                    if (i == waypointIndex) 
                    {
                        Gizmos.DrawLine(transform.position, waypointPath[i]);
                    }
                    else 
                    {
                        Gizmos.DrawLine(waypointPath[i-1], waypointPath[i]);
                    }
                }
            }
        }
	}
}