using UnityEngine;
using System.Collections;

namespace Units
{
	public class UnitManager : MonoBehaviour 
	{
		// General parameters
		public string Name = "";
		public string Code = "";
		public bool isGroundUnit = false;
		public bool isAirUnit = false;

		// Movement parameters
		public float MovingSpeed = 0;
		public float RotationSpeed = 0;

		// Attack parameters
		public bool CanAttackGround = false;
		public bool CanAttackAir = false;
		public float MinAttackRange = 0;
		public float MaxAttackRange = 0;


		public void Attack(GameObject target)
		{
			if (target == null)
			{
				AttackDelegate(null);
			}
			else if (target == this.gameObject)
			{
				// Do nothing if unit to attack is itself
				return;
			}
			else
			{
				UnitManager targetUnitManager = target.GetComponent<UnitManager>();
				if (targetUnitManager != null)
				{				
					if (RespectsAttackTypes(targetUnitManager) && InAttackRange(target))
					{
						AttackDelegate(target);
					}
				}
			}
		}


		private bool RespectsAttackTypes(UnitManager targetUnitManager)
		{
			return ((targetUnitManager.isAirUnit && this.CanAttackAir) || (targetUnitManager.isGroundUnit && this.CanAttackGround));
		}


		private bool InAttackRange(GameObject target) 
		{
			double distance = (this.transform.position - target.transform.position).magnitude;
			if (distance >= MinAttackRange && distance <= MaxAttackRange) return true;
			return false;
		}


		// Function that delegates "Attack" calls/requirements to sub-classes of the GameObject linked to this "UnitManager" class
		private void AttackDelegate(GameObject target)
		{
			if (this.gameObject.tag == "Tank")
			{
				this.GetComponent<TankManager>().AimingTarget = target;
			}
		}
	}
}