// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;

namespace Units 
{	
	[RequireComponent(typeof(UnitManager))]
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

		private UnitManager unitManager;
		private float barrelRotationDelta;
        private float currentAttackDelay = 0;


		void Awake ()
		{
			unitManager = GetComponent<UnitManager>();
			barrelRotationDelta = BarrelRotateSpeed * Time.deltaTime;

            // Instanciate bullet from reference prefab and hide it
            if (AttackBullet != null)
            {
                AttackBullet = Instantiate(AttackBullet);
                ProjectileVisibility = false;
            }
		}


		void Update ()
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
			if (AimingTarget == null)
			{
				targetDirection = transform.forward;
			}
			else
			{
				// Angle for turret rotation (Y)
				targetDirection = AimingTarget.transform.position - CannonTurret.transform.position;
				targetDirection.y = CannonTurret.transform.position.y;
			}

			// Apply the required angle to turn toward the desired position with a rotation delay
			//  - If rotate speed is zero (cannot rotate turret, turn the whole tank toward the target)
			//  - Otherwise, only rotate the turret toward the target
			Quaternion turretRelativeAngleY = Quaternion.LookRotation(targetDirection, Vector3.up);
			if (CannonRotateSpeed == 0)
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation, turretRelativeAngleY, unitManager.RotationSpeed * Time.deltaTime);
			}
			else
			{
				CannonTurret.transform.rotation = Quaternion.RotateTowards(CannonTurret.transform.rotation, turretRelativeAngleY, CannonRotateSpeed * Time.deltaTime);
			}
		}


		private void AdjustCannonBarrelRotation() 
		{	
			Quaternion barrelRelativeAngleX;	
			if (BarrelLockOnTarget && AimingTarget != null)
			{
				// For lock on specified target, rotate the barrel to point toward it 
				barrelRelativeAngleX = Quaternion.LookRotation(AimingTarget.transform.position - CannonTurret.transform.position, CannonBarrel.transform.up);
			}
			else if (!BarrelLockOnTarget && AimingTarget != null)
			{
				// For specific rotation angle, find remaining upward rotation of barrel and apply offsets until reached
				float currentBarrelAngle = Vector3.Angle(CannonTurret.transform.forward, CannonBarrel.transform.forward);
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
				barrelRelativeAngleX = Quaternion.LookRotation(CannonTurret.transform.forward, CannonBarrel.transform.up);
			}
			// Apply the rotation
			CannonBarrel.transform.rotation = Quaternion.RotateTowards(CannonBarrel.transform.rotation, barrelRelativeAngleX, barrelRotationDelta);
		}


        private bool ProjectileVisibility
        {
            set { AttackBullet.SetActive(value); }
            get { return AttackBullet.activeSelf; }
        }


        private void ExecuteProjectileAnimation()
        {
            // Execute the animation only if a target and projectile were specified, and the delay between shots is over
            if (AimingTarget != null && unitManager.InAttackRange(AimingTarget) && 
                AttackBullet != null && currentAttackDelay <= 0)
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

                // Shoot the projectile only if aiming toward the target, and at the right upward angle when applicable
                // Apply required trajectory (arc or linear) according to barrel operation mode
                if (angleAwayFromTarget <= unitManager.PermissiveDestinationAngleDelta)
                {                       
                    if (BarrelAttackAngle == 0)
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        Debug.Log("FIRE LIN!");
                        #endregion
                        #endif

                        StartCoroutine(LinearProjectileAnimation());
                    }
                    else if (barrelUpwardAngle == BarrelAttackAngle)
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        Debug.Log("FIRE ARC!");
                        #endregion
                        #endif

                        StartCoroutine(ArcProjectileAnimation());
                    }

                    // Reset timer for the next attack allowed
                    currentAttackDelay = AttackMinInterval;
                }
            }
        }


        private IEnumerator LinearProjectileAnimation()
        {
            // Display the projectile and place it at the barrel exit 
            ProjectileVisibility = true;
            AttackBullet.transform.position = BarrelOutput.transform.position;
            AttackBullet.transform.rotation = BarrelOutput.transform.rotation;

            // Linear trajectory of the projectile until the target is reached
            while (AttackBullet.transform.position != AimingTarget.transform.position)
            {
                AttackBullet.transform.position = Vector3.MoveTowards(AttackBullet.transform.position, 
                                                                      AimingTarget.transform.position,
                                                                      AttackBulletSpeed * Time.deltaTime);
                yield return null;
            }

            //Hide the projectile when the target was reached
            ProjectileVisibility = false;
        }


        private IEnumerator ArcProjectileAnimation()
        {          
            // Display the projectile and place it at the barrel exit 
            ProjectileVisibility = true;
            AttackBullet.transform.position = BarrelOutput.transform.position;
            AttackBullet.transform.rotation = BarrelOutput.transform.rotation;

            // Get the starting conditions of the arc trajectory
            var startPosition = BarrelOutput.transform.position;
            var endPosition = AimingTarget.transform.position;
            var initialHeight = BarrelOutput.transform.position.y;
            var incTime = 0f;
            Vector3 prevPosition = startPosition;

            while (AttackBullet.transform.position != endPosition)
            {
                // Update the new projectile position along the arc trajectory
                incTime += Time.deltaTime;
                var currentBulletPosition = Vector3.Lerp(startPosition, endPosition, incTime);
                currentBulletPosition.y += initialHeight * 2 * Mathf.Sin(Mathf.Clamp01(incTime) * Mathf.PI);

                // Adjust projectile angle to point toward the front following the arc trajectory (tangent)
                AttackBullet.transform.position = currentBulletPosition;
                AttackBullet.transform.rotation = Quaternion.LookRotation(currentBulletPosition - prevPosition);
                prevPosition = currentBulletPosition;   // Update for next tangent calculation

                yield return null;
            }

            //Hide the projectile when the target was reached
            ProjectileVisibility = false;
        }
	}
}