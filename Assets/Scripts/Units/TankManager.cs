using UnityEngine;
using System.Collections;

namespace Units 
{	
	public class TankManager : MonoBehaviour
	{		
		public GameObject LookAtTarget;		// Specify a location to make the cannon look at (ennemy, building, default: forward if null)
		public GameObject CannonTurret;		// Reference to cannon section of the tank
		public GameObject CannonBarrel;		// Reference to barrel section of the tank (shooting)
		public float CannonRotateSpeed;		// Cannon rotation speed in deg/s
		public float CannonAttackRange;		// Minimum distance required to allow attacking
		public string Name = "";			// Tank general name
		public string Code = "";			// Tank code name/number

		void Awake ()
		{

		}
		

		void Update ()
		{				
			transform.RotateAround(transform.position, transform.up, 90 * Time.deltaTime);
			AdjustCannonRotation();
		}


		private void AdjustCannonRotation()
		{
			// Get the direction where the cannon has to look at:
			// 	- If a target to attack is specified, look toward it
			// 	- If there is no target, look forward
			Vector3 targetDirection;
			if (LookAtTarget == null)
			{
				targetDirection = transform.forward;
			}
			else
			{
				targetDirection = LookAtTarget.transform.position - CannonTurret.transform.position;
				targetDirection.y = CannonTurret.transform.position.y;
			}
			// Apply the required angle to move to the desired position with a rotation delay (CannonRotateSpeed)
			Quaternion relativeAngleTarget = Quaternion.LookRotation(targetDirection, Vector3.up);
			CannonTurret.transform.rotation = Quaternion.RotateTowards(CannonTurret.transform.rotation, relativeAngleTarget, CannonRotateSpeed * Time.deltaTime);
		}
	}
}