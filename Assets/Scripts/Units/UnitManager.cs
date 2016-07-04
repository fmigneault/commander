﻿// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

		// Unit production parameters
		public float ProductionDelay = 0; 

		// Movement parameters
		public float MovingSpeed = 0;
		public float RotationSpeed = 0;

		// Attack parameters
		public bool CanAttackGround = false;
		public bool CanAttackAir = false;
		public float MinAttackRange = 0;
		public float MaxAttackRange = 0;

		// Selected unit highlight on ground reference
		public GameObject SelectionSprite = null;

        // Icon used to represent the unit on the Mini-Map
        public SpriteRenderer MiniMapIconSprite = null;

        // Combinations of particle systems used to simulate the destruction or movement of the unit
        public GameObject DestroyExplosionEffect = null;
        public GameObject MovementEffect = null;

        // Parameters for building construction (only if "builder" unit)
		public List<GameObject> ProducedBuildings = null;

        // Internal memory of requested destination
        //    Important: Temporarily assign origin position, but will be updated on 'Start' (see reason below)
        private Vector3 destinationRequest = Vector3.zero;

        // Internal memory of attack target
        private GameObject attackTarget = null;


		void Awake()
		{
            // Initialize components and visual effects hidden
			InitializeSelectionHighlight();
            InitializeMiniMapIcon();
            InitializeParticleEffects();

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
            UpdateAttackUnit();
            if (!UpdateRotateUnit()) return;
            UpdateMoveUnit();
        }


        // Function for outside calls to request new destinations 
        public void MoveToDestination(Vector3 destination)
        {      
            // Stop attacking if it was, then request to move
            AttackTarget(null);
            destinationRequest = destination;

            #if OUTPUT_DEBUG
            #region DEBUG
            Debug.Log("Moving");
            #endregion
            #endif
        }


        // Function for outside calls to request new target to attack
        public void AttackTarget(GameObject target)
        {        
            // Do nothing if requested unit to attack is itself    
            if (target != gameObject) attackTarget = target;
        }   


        private void UpdateMoveUnit()
        {            
            var newPosition = Vector3.MoveTowards(transform.position, destinationRequest, MovingSpeed * Time.deltaTime);
            if (newPosition != transform.position)
            {
                transform.position = newPosition;
                MovementEffect.transform.position = newPosition;
                MovementEffect.transform.rotation = transform.rotation;
                StartCoroutine(EffectManager.LoopParticleSystems(MovementEffect));
            }
            else StartCoroutine(EffectManager.StopParticleSystemsCompleteAnimation(MovementEffect));
        }


        private bool UpdateRotateUnit()
        {     
            var towardDestination = destinationRequest - transform.position;
            var angleFromDestination = Vector3.Angle(towardDestination, transform.forward);
            if (angleFromDestination > PermissiveDestinationAngleDelta && !towardDestination.Equals(Vector3.zero))
            {   
                // Stop the particle emission if rotation is needed, otherwise, it makes a weird visible effect where 
                // the particles suddenly rotate when the next emission is requested as the forward movement resumes
                StartCoroutine(EffectManager.StopParticleSystemsCompleteAnimation(MovementEffect));

                var rotationDestination = Quaternion.LookRotation(towardDestination, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDestination, RotationSpeed * Time.deltaTime);
                return false;
            }
            return true;
        }


        private void UpdateAttackUnit()
        {
            if (attackTarget != null)
            {
                UnitManager targetUnitManager = attackTarget.GetComponent<UnitManager>();
                if (targetUnitManager != null && RespectsAttackTypes(targetUnitManager))
                {
                    #if OUTPUT_DEBUG
                    #region DEBUG
                    if (tag == "TemperedHammer") Debug.Log(string.Format("Respect Type + Target ({0})", attackTarget == null));
                    #endregion
                    #endif

                    // If not in range, move to minimum/maximum required attack range first
                    if (!InAttackRange(attackTarget))
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        if (tag == "TemperedHammer") Debug.Log(string.Format("Out of Range - Moving first ({0})", attackTarget == null));
                        #endregion
                        #endif

                        destinationRequest = GetRequiredPositionInRange(attackTarget);
                    }
                    // Otherwise, cancel movement
                    // This allows attacking right away an in-range unit
                    else
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        if (tag == "TemperedHammer") Debug.Log(string.Format("In Range - Stop moving ({0})", attackTarget == null));
                        #endregion
                        #endif

                        destinationRequest = transform.position;
                    }
                }
            }

            // Attack when in range or cancel attack if null
            AttackDelegate(attackTarget);

            #if OUTPUT_DEBUG
            #region DEBUG
            if (tag == "TemperedHammer") Debug.Log("In Range - Attacking");
            #endregion
            #endif
        }
	

		private bool RespectsAttackTypes(UnitManager targetUnitManager)
		{
			return ((targetUnitManager.IsAirUnit && CanAttackAir) || (targetUnitManager.IsGroundUnit && CanAttackGround));
		}


		public bool InAttackRange(GameObject target) 
		{
			double distance = Vector3.Distance(transform.position, target.transform.position);
			if (distance >= MinAttackRange && distance <= MaxAttackRange) return true;
			return false;
		}


        private Vector3 GetRequiredPositionInRange(GameObject target)
        {            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < MinAttackRange) return (transform.position - target.transform.position) * MinAttackRange / distance;
            return Vector3.Lerp(target.transform.position, transform.position, MaxAttackRange / distance);
        }


		// Function that delegates "Attack" calls/requirements to sub-classes of the GameObject linked to this "UnitManager" class
		private void AttackDelegate(GameObject target)
		{
			if (gameObject.tag == "Tank")
			{
				GetComponent<TankManager>().AimingTarget = target;
			}
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


        private void InitializeParticleEffects()
        {
            // Initialize particle effects to be used with 'EffectManager', or set to null if not possible
            DestroyExplosionEffect = EffectManager.InitializeParticleSystems(DestroyExplosionEffect);
            MovementEffect = EffectManager.InitializeParticleSystems(MovementEffect);
        }


        public void DisplayUnitDestruction()
        {
            if (DestroyExplosionEffect != null)
            {
                DestroyExplosionEffect.transform.position = transform.position;
                StartCoroutine(EffectManager.PlayParticleSystems(DestroyExplosionEffect));
            }
        }
	}
}