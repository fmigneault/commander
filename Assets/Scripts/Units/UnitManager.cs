using UnityEngine;
using System.Collections.Generic;

namespace Units
{
	public class UnitManager : MonoBehaviour 
	{
		// General parameters
		public string Name = "";
		public string Code = "";
		public Color FactionColor = Color.white;
		public bool isGroundUnit = false;
		public bool isAirUnit = false;

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

		// Parameters for building construction (only if "construction" unit)
		public List<GameObject> ProducedBuildings = null;


		void Start ()
		{
			InitializeSelectionHighlight();
		}
				

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
	}
}