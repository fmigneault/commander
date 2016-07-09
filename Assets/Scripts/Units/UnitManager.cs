// Display debugging/logging info on console
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
        //    Important: Temporarily assign origin position, but will be updated on 'Start' (see reason below)
        private Vector3 destinationRequest = Vector3.zero;
        private Vector3[] destinationWaypointPath = null;
        private int destinationWaypointIndex = 0;

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


        void Update()
        {      
            // Update health, then apply actions accordingly to specified commands and current statuses
            if (HealthBar != null) HealthBar.Health = (float)Health / (float)maxHealth;
            if (Health > 0)
            {                
                UpdateAttackUnit();
                if (!UpdateRotateUnit()) return;
                UpdateMoveUnit();
            }
            else
            {
                StartCoroutine(DestroyUnit());
            }
        }


        // Function for outside calls to request new destinations
        public void MoveToDestination(Vector3 destination)
        {      
            // Stop attacking if it was, then request to move
            AttackTarget(null);

            // Request a new pathfinding search to get path waypoints
            PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
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
            return true;    // Finished rotation (already pointing toward target)
        }


        private void UpdateAttackUnit()
        {
            if (attackTarget != null)
            {
                UnitManager targetUnitManager = attackTarget.GetComponent<UnitManager>();
                if (targetUnitManager != null)
                {
                    if (targetUnitManager.Health < 0)
                    {
                        attackTarget = null;
                    }
                    else if (RespectsAttackTypes(targetUnitManager))
                    {
                        // If not in range, move to minimum/maximum required attack range first
                        if (!InAttackRange(attackTarget))
                        {
                            // Request a new pathfinding search to get path waypoints only as necessary
                            //    A new request is needed if the assigned target has changed, or if the previously
                            //    found final waypoint in the path would make the unit out-of-range when reaching this
                            //    final destination (ei: target has sufficiently moved away from its previous location)
                            bool targetChangedPosition = false;
                            if (destinationWaypointPath != null && destinationWaypointPath.Length > 0)
                            {
                                var lastKnowDestination = destinationWaypointPath[destinationWaypointPath.Length - 1];
                                var targetPosition = attackTarget.transform.position;
                                targetChangedPosition = !InAttackRange(targetPosition, lastKnowDestination);
                            }
                            if (newAttackCommand || targetChangedPosition)
                            {
                                var destinationInRange = GetRequiredPositionInRange(attackTarget);
                                PathRequestManager.RequestPath(transform.position, destinationInRange, OnPathFound);
                                newAttackCommand = false;   // Reset for next calls
                            }
                        }
                        // If already within attack range, cancel any movement
                        //    This allows attacking right away the target without moving the unit
                        else destinationRequest = transform.position;
                    }
                    else attackTarget = null;   // Reject any set target if it doesn't match any condition
                }
            }

            // Attack when in range or cancel attack if null
            AttackDelegate(attackTarget);
        }
	

		private bool RespectsAttackTypes(UnitManager targetUnitManager)
		{
			return ((targetUnitManager.IsAirUnit && CanAttackAir) || 
                    (targetUnitManager.IsGroundUnit && CanAttackGround));
		}


        private bool InAttackRange(Vector3 targetPosition, Vector3 referencePosition)
        {
            double distance = Vector3.Distance(referencePosition, targetPosition);
            if (distance >= MinAttackRange && distance <= MaxAttackRange) return true;
            return false;
        }


		public bool InAttackRange(GameObject target) 
		{            
            return InAttackRange(target.transform.position, transform.position);
		}


        private Vector3 GetRequiredPositionInRange(GameObject target)
        {            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < MinAttackRange)
            {
                return (transform.position - target.transform.position) * MinAttackRange / distance;
            }
            return Vector3.Lerp(target.transform.position, transform.position, MaxAttackRange / distance);
        }


		// Function that delegates "Attack" calls to sub-classes of the GameObject linked to this "UnitManager" class
		private void AttackDelegate(GameObject target)
		{
			if (gameObject.tag == "Tank") GetComponent<TankManager>().AimingTarget = target;
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

            // Destroy all instanciated references, then destoy the actual unit
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


        public float PermissiveDestinationAngleDelta
        {
            get;
            private set;
        }


        // Reference to an attached HealthBar object (searched automatically at initialization)
        public HealthBarManager HealthBar
        {
            get;
            private set;
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


        // Callback method for when the path request has finished being processed 
        public void OnPathFound(Vector3[] newPath, bool pathSuccess)
        {
            if (pathSuccess)
            {                
                destinationWaypointPath = newPath;  // Update the found path waypoints
                destinationWaypointIndex = 0;       // Start at the first waypoint position as a destination
                StopCoroutine("FollowPath");        // Stop updating the waypoints of a previous path request
                StartCoroutine("FollowPath");       // Gradually update the waypoints of the new path request
            }
        }


        // Assigns the next waypoint in the path as a new destination when the current one is reached by the unit
        IEnumerator FollowPath() 
        {     
            var currentWaypoint = destinationWaypointPath[0];
            while (true) 
            {
                if (transform.position == currentWaypoint) 
                {
                    destinationWaypointIndex++;
                    if (destinationWaypointIndex >= destinationWaypointPath.Length) 
                    {
                        yield break;
                    }
                    currentWaypoint = destinationWaypointPath[destinationWaypointIndex];
                }

                destinationRequest = currentWaypoint;
                yield return null;
            }
        }


        // Draws the waypoints with connected lines to form the returned path to avoid obstables
        public void OnDrawGizmos()
        {
            if (destinationWaypointPath != null) 
            {
                for (int i = destinationWaypointIndex; i < destinationWaypointPath.Length; i++) 
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(destinationWaypointPath[i], Vector3.one);

                    if (i == destinationWaypointIndex) 
                    {
                        Gizmos.DrawLine(transform.position, destinationWaypointPath[i]);
                    }
                    else 
                    {
                        Gizmos.DrawLine(destinationWaypointPath[i-1], destinationWaypointPath[i]);
                    }
                }
            }
        }
	}
}