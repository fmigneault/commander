using UnityEngine;
using System.Collections;

namespace Units 
{	
	[RequireComponent(typeof(Units.UnitManager))]
	public class TankManager : MonoBehaviour
	{		
		public GameObject AimingTarget;		// Specify a location to make the cannon look at (ennemy, building, default: forward if null)
		public GameObject CannonTurret;		// Reference to cannon section of the tank
		public GameObject CannonBarrel;		// Reference to barrel section of the tank (shooting, pivot has to be at connection with turret)
		public float CannonRotateSpeed;		// Cannon rotation speed in deg/s (zero if cannot rotate independantly, ie: turn the whole tank)
		public float BarrelRotateSpeed;		// Upward rotation speed in deg/s (zero if cannot rotate barrel up/down)
		public bool BarrelLockOnTarget;		// Specify if the barrel needs to aim at the target to attack
		public float BarrelAttackAngle;		// Required angle of barrel to attack
		public string Code = "";			// Tank code name/number

		private Units.UnitManager unitManager;
		private float barrelRotationDelta;

		void Awake ()
		{
			unitManager = GetComponent<Units.UnitManager>();
			barrelRotationDelta = BarrelRotateSpeed * Time.deltaTime;
		}
		

		void Update ()
		{				
			AdjustCannonTurretRotation();
			AdjustCannonBarrelRotation();
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


		public void AdjustCannonBarrelRotation() 
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
	}
}