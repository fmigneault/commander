// Display debugging/logging info on console
//#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;

namespace Units 
{		
	public class TankManager : MonoBehaviour
	{		        
		public GameObject AimingTarget;		// Specify a location to make the cannon look at (ennemy, building, default: forward if null)
        public GameObject AttackBullet;     // Prefab that will act as a projectile fired when attacking 
        public float AttackBulletSpeed;     // Speed at which the projectile is fired
        public float AttackMinInterval;     // Minimum delay required until next attack is permitted (fire next bullet)
        public GameObject BarrelOutput;     // Position to fire the projectile from the barrel (bullet start position)
        public GameObject CannonTurret;		// Reference to cannon section of the tank
        public GameObject CannonBarrel;		// Reference to barrel section of the tank (shooting, pivot has to be at connection with turret)
		public float CannonRotateSpeed;		// Cannon rotation speed in deg/s (zero if cannot rotate independantly, ie: turn the whole tank)
		public float BarrelRotateSpeed;		// Upward rotation speed in deg/s (zero if cannot rotate barrel up/down)
		public bool BarrelLockOnTarget;		// Specify if the barrel needs to aim at the target to attack
		public float BarrelAttackAngle;		// Required angle of barrel to attack

        // Combinations of particle systems used to simulate a projectile impact/shooting toward a target unit
        // Specify different hit impact effects according to trajectory types
        public GameObject ProjectileImpactEffect = null;
        public GameObject ProjectileShootEffect = null;

		private UnitManager unitManager;
		private float barrelRotationDelta;
        private float currentAttackDelay = 0;

        // Trajectory function type
        private delegate IEnumerator Trajectory();


		void Awake()
		{
            // Reference validation
			unitManager = GetComponent<UnitManager>();
            if (unitManager == null) throw new MissingComponentException("Missing 'UnitManager' component");

            // Initialize parameters and display
			barrelRotationDelta = BarrelRotateSpeed * Time.deltaTime;
            InitializeProjectileAndEffects();
		}


		void Update()
		{				            
            // Update attack timer
            currentAttackDelay = Mathf.Clamp(currentAttackDelay - Time.deltaTime, 0, currentAttackDelay);

			AdjustCannonTurretRotation();
			AdjustCannonBarrelRotation();
            ExecuteProjectileAnimation();
		}


		private void AdjustCannonTurretRotation()
		{
			Vector3 targetDirection;

			// Get the direction where the cannon has to look at:
			// 	- If there is no target, look forward (normal position)
			// 	- If a target to attack is specified, look toward it and adjust barrel angle if required
            //  - If the unit requires whole body rotation to align its cannon toward the target, it must be 
            //    explicitly asked to aim at it otherwise the 'else' condition locks the unit toward the target.
            if (AimingTarget == null || (!AimBarrel && LockedTurretRotation))
			{
				targetDirection = transform.forward;
			}
			else
			{				
				targetDirection = AimingTarget.transform.position - CannonTurret.transform.position;
				targetDirection.y = CannonTurret.transform.position.y;
			}

			// Apply the required angle to turn toward the desired position with a rotation delay
			//  - Turn the whole tank toward the target if its turret rotation is locked
			//  - Otherwise, only rotate the turret toward the target
			Quaternion turretRelativeAngleY = Quaternion.LookRotation(targetDirection, Vector3.up);
            if (LockedTurretRotation)
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation, turretRelativeAngleY, 
                                                              unitManager.RotationSpeed * Time.deltaTime);
			}
			else
			{
				CannonTurret.transform.rotation = Quaternion.RotateTowards(CannonTurret.transform.rotation, 
                                                                           turretRelativeAngleY, 
                                                                           CannonRotateSpeed * Time.deltaTime);
			}
		}


		private void AdjustCannonBarrelRotation() 
		{	            
			Quaternion barrelRelativeAngleX;	
			if (BarrelLockOnTarget && AimingTarget != null && AimBarrel)
			{
				// For lock on specified target, rotate the barrel to point toward it 
                var towardTarget = AimingTarget.transform.position - CannonTurret.transform.position;
				barrelRelativeAngleX = Quaternion.LookRotation(towardTarget, CannonBarrel.transform.up);
			}
            else if (!BarrelLockOnTarget && AimingTarget != null && AimBarrel)
			{
				// For specific rotation angle, find remaining upward rotation of barrel and apply offsets until reached
				float currentBarrelAngle = Vector3.Angle(CannonTurret.transform.forward, 
                                                         CannonBarrel.transform.forward);
                float rotationOffset = currentBarrelAngle - BarrelAttackAngle;
				if (Mathf.Abs(rotationOffset) > barrelRotationDelta)
				{ 
					rotationOffset = -barrelRotationDelta;
				}
				CannonBarrel.transform.Rotate(rotationOffset, 0, 0);
				return;
			}
			else
			{
				// Otherwise aim forward in normal position (no target)
				barrelRelativeAngleX = Quaternion.LookRotation(CannonTurret.transform.forward, 
                                                               CannonBarrel.transform.up);
			}
			// Apply the rotation
			CannonBarrel.transform.rotation = Quaternion.RotateTowards(CannonBarrel.transform.rotation, 
                                                                       barrelRelativeAngleX, barrelRotationDelta);
		}


        //  If rotate speed is zero, the unit cannot rotate turret by default
        public bool LockedTurretRotation { get { return CannonRotateSpeed.Equals(0); } }


        public bool AimBarrel { get; set; }


        private bool ProjectileVisibility
        {
            set { AttackBullet.SetActive(value); }
            get { return AttackBullet.activeSelf; }
        }


        public bool HoldFire { get; set; }


        private void ExecuteProjectileAnimation()
        {
            // If hold-fire command is set, don't shoot (but aiming at target still handled by other the functions)
            if (HoldFire) return;

            UnitManager targetUnitManager = null;
            if (AimingTarget != null) targetUnitManager = AimingTarget.GetComponent<UnitManager>();;

            // Run the animation only if all the conditions are met: 
            //    There is a target with remaining health to attack, a projectile is specified, the unit to attack is
            //    within attack range, the current unit is not destroyed and the delay between shots fired has elapsed
            if (targetUnitManager != null && targetUnitManager.Health > 0 && AttackBullet != null && 
                unitManager.InAttackRange(AimingTarget.transform.position) && unitManager.Health > 0 && 
                currentAttackDelay <= 0)
            {
                // Get the angle between the barrel output and the target
                var barrelTowardTarget = AimingTarget.transform.position - CannonBarrel.transform.position;
                var barrelAlongTerrain = BarrelOutput.transform.forward;
                barrelTowardTarget.y = 0;       // Suppress height variation because we want XZ angle
                barrelAlongTerrain.y = 0;       // Idem
                var angleAwayFromTarget = Vector3.Angle(barrelTowardTarget, barrelAlongTerrain);

                // Get the angle between the barrel forward resting position and the current barrel pointing upward
                var barrelUpwardAngle = Vector3.Angle(transform.forward, BarrelOutput.transform.forward);

                #if OUTPUT_DEBUG
                #region DEBUG
                Debug.Log(string.Format("Target Angle: {0}, Upward Angle: {1}",angleAwayFromTarget,barrelUpwardAngle));
                #endregion
                #endif

                // Shoot the projectile only if aiming toward the target and at the right upward angle as applicable
                // Apply required trajectory (arc or linear) according to barrel operation mode
                if (angleAwayFromTarget <= unitManager.PermissiveDestinationAngleDelta)
                {                       
                    if (BarrelAttackAngle == 0)
                    {
                        StartCoroutine(ProjectileAnimation(LinearTrajectory));
                    }
                    else if (barrelUpwardAngle == BarrelAttackAngle)
                    {
                        StartCoroutine(ProjectileAnimation(ArcTrajectory));
                    }

                    // Reset timer for the next attack allowed
                    currentAttackDelay = AttackMinInterval;
                }
            }
        }


        private IEnumerator ProjectileAnimation(Trajectory trajectory)
        {
            // Display the projectile and place it at the barrel exit 
            ProjectileVisibility = true;
            AttackBullet.transform.position = BarrelOutput.transform.position;
            AttackBullet.transform.rotation = BarrelOutput.transform.rotation;
            DisplayProjectileEffect(AttackBullet, ProjectileShootEffect);

            // Execute the trajectory animation up to the target location
            yield return trajectory();

            // Hide the projectile when the target was reached and display the impact effect at it's location
            //    Use the current's unit impact effects even if it is the target that gets hit to ensure that each 
            //    projectile gets the corresponding impact on hit, in case that multiple units attack the same target.
            //    Using the target's effect could interfere with another impact already occuring and using it, but 
            //    only a single impact can happen at a time for the attacking unit since we limit attacks over a delay.
            ProjectileVisibility = false;
            DisplayProjectileEffect(AttackBullet, ProjectileImpactEffect);

            // Reduce the target's health by the amount of hit points
            //    In case that multiple units are attacking the same target and that another unit's projectile destroyed 
            //    the target before this unit's projectile reached it, the reference to the target could already have
            //    been unset because the target is not in 'destroyed' state. We need to check the reference beforehand.
            if (AimingTarget != null) AimingTarget.GetComponent<UnitManager>().Health -= unitManager.HitPoints;
        }


        IEnumerator ArcTrajectory() 
        {
            // Get the starting conditions of the arc trajectory
            var startPosition = BarrelOutput.transform.position;
            var endPosition = AimingTarget.transform.position;
            var initialHeight = BarrelOutput.transform.position.y;
            var incTime = 0f;
            Vector3 prevPosition = startPosition;

            while (AttackBullet.transform.position != endPosition)
            {
                // Get the time interval
                //    DeltaTime could simply be used, but to allow variations of projectile speed using the input 
                //    parameter, we use it as a proportion of 100, which is around the same speeds for linear trajectory
                incTime += Time.deltaTime * AttackBulletSpeed / 100;

                // Update the new projectile position along the arc trajectory
                var currentBulletPosition = Vector3.Lerp(startPosition, endPosition, incTime);
                currentBulletPosition.y += initialHeight * 2 * Mathf.Sin(Mathf.Clamp01(incTime) * Mathf.PI);

                // Adjust projectile angle to point toward the front following the arc trajectory (tangent)
                AttackBullet.transform.position = currentBulletPosition;
                AttackBullet.transform.rotation = Quaternion.LookRotation(currentBulletPosition - prevPosition);
                prevPosition = currentBulletPosition;   // Update for next tangent calculation

                yield return null;
            }
        }


        IEnumerator LinearTrajectory() 
        {
            // Linear trajectory of the projectile until the target is reached
            //    Use a temporary target reference since another command received like moving the unit while the attack
            //    animation has already started will set the reference to null and cause an error, in turn not hiding 
            //    the projectile and creating the impact effect.
            var aimingTargetPosition = AimingTarget.transform.position;
            while (AttackBullet.transform.position != aimingTargetPosition)
            {
                AttackBullet.transform.position = Vector3.MoveTowards(AttackBullet.transform.position,
                                                                      aimingTargetPosition, 
                                                                      AttackBulletSpeed * Time.deltaTime);
                yield return null;
            }
        }


        // Displays the projectile effect at the current location of the specified projectile
        private void DisplayProjectileEffect(GameObject projectile, GameObject effect)
        {
            if (projectile != null && effect != null)
            {
                // Set the impact position and orientation according to the projectile direction, the display effects
                effect.transform.position = projectile.transform.position;
                effect.transform.rotation = projectile.transform.rotation;
                StartCoroutine(EffectManager.PlayParticleSystems(effect));
            }
        }


        private void InitializeProjectileAndEffects()
        {
            // Instanciate projectile from reference prefab and hide it
            // Instanciate projectile effects only if a projectile was specified, otherwise they are not required
            if (AttackBullet != null)
            {
                AttackBullet = Instantiate(AttackBullet);
                ProjectileVisibility = false;

                // Initialize particle effects to be used with 'EffectManager', or set to null if not possible
                ProjectileImpactEffect = EffectManager.InitializeParticleSystems(ProjectileImpactEffect);
                ProjectileShootEffect = EffectManager.InitializeParticleSystems(ProjectileShootEffect);
            }
        }


        public void DestroyInstanciatedReferences()
        {
            if (ProjectileImpactEffect != null) Destroy(ProjectileImpactEffect);
            if (ProjectileShootEffect != null) Destroy(ProjectileShootEffect);
        }
	}
}