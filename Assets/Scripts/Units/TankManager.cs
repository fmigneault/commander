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
            if (AimingTarget != null && AttackBullet != null && currentAttackDelay <= 0)
            {
                // Get the angle between the barrel and the target
                // Shoot the projectile only if properly aiming at the target
                var barrelTowardTarget = CannonBarrel.transform.position - AimingTarget.transform.position;
                barrelTowardTarget.y = 0;   // Suppress height variation because we want XZ angle
                var angleAwayFromTarget = Vector3.Angle(barrelTowardTarget, CannonBarrel.transform.forward);
                Debug.Log(string.Format("Angle: {0}",angleAwayFromTarget));
                if (angleAwayFromTarget == 0)
                {   
                    // Apply required trajectory (arc or linear) according to barrel operation mode
                    if (BarrelAttackAngle == 0)
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        Debug.Log("FIRE LIN!");
                        #endregion
                        #endif

                        StartCoroutine(LinearProjectileAnimation());
                    }
                    else
                    {
                        #if OUTPUT_DEBUG
                        #region DEBUG
                        Debug.Log("FIRE ARC!");
                        #endregion
                        #endif

                        //################### ARC TRAJECTORY
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
	}
}