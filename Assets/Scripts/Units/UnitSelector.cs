// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS_Cam;

namespace Units
{
	[RequireComponent(typeof(RTS_Camera))]
	public class UnitSelector : MonoBehaviour 
	{
		public string[] SelectTags;		// Tags of GameObjects which unit selection is permitted
		public string[] AttackTags;		// Tags of GameObjects which unit attack command is permitted

		// Key mappings
		public KeyCode multipleSelectKey = KeyCode.LeftControl;
		public KeyCode unitSelectionKey = KeyCode.Mouse0;
		public KeyCode unitAttackMoveKey = KeyCode.Mouse1;

		private float maxDistance;
		private Camera cam;
		List<GameObject> selectedUnits;

		void Start ()
		{
			cam = gameObject.GetComponent<RTS_Camera>().GetComponent<Camera>();
			maxDistance = gameObject.GetComponent<RTS_Camera>().maxHeight * 2;	
			selectedUnits = new List<GameObject>();
		}
		

		void Update () 
		{			
			Vector3 mousePosition = Input.mousePosition;

			if (Input.GetKeyUp(unitSelectionKey))
			{								
				SelectUnits(mousePosition, Input.GetKey(multipleSelectKey));
			}
			else if (Input.GetKeyUp(unitAttackMoveKey))
			{
				bool attaking = AttackUnit(mousePosition);
				if (!attaking) MoveUnit(mousePosition);
			}
		}


		private void SelectUnits(Vector3 mousePosition, bool multipleSelect=false)
		{
			if (SelectTags != null)
			{		
				// Draw ray from camera toward pointed position, execute command only on hit (any tag)
				Ray ray = cam.ScreenPointToRay(mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance))
				{
					// Verify if the hit gameobject tag is one of the selectable unit 
					bool anySelected = false;
					foreach (var tag in SelectTags)
					{
						if (hit.transform.CompareTag(tag) && hit.collider != null)
						{	
							// Get newly pointed unit selection
							GameObject unit = hit.collider.gameObject;

							// If not using multiple selection, un-select previously selected units
							if (!multipleSelect) UnSelectAllUnits();						

							// If using multiple selection, un-select already selected unit
							if (multipleSelect && selectedUnits.Contains(unit))
							{
								UnselectSingleUnit(unit);
								return;
							}

							// Update unit selection
							SetUnitHighlightState(unit, true);
							selectedUnits.Add(unit);
							anySelected = (selectedUnits.Count > 0);

							#if OUTPUT_DEBUG
							#region DEBUG
							if (unit != null && unit.GetComponent<UnitManager>() != null) 
							{
								string hitName = unit.GetComponent<UnitManager>().Name;
								Debug.Log(string.Format("Selected: {0}, {1}", tag, hitName));
							}
							#endregion
							#endif
						}

						// If no valid unit tag was hit, unselect units (ex: click on terrain)
						if (!anySelected) UnSelectAllUnits();
					}					
				}	
			}
		}

	
		private void UnSelectAllUnits()
		{
			foreach (var unit in selectedUnits)
			{
				SetUnitHighlightState(unit, false);
			}
			selectedUnits.Clear();
		}


		private void UnselectSingleUnit(GameObject unit) 
		{			
			selectedUnits.Remove(unit);
			SetUnitHighlightState(unit, false);
		}


		private void SetUnitHighlightState(GameObject unit, bool state) 
		{
			if (unit != null)
			{
				UnitManager unitManager = unit.GetComponent<UnitManager>();
				if (unitManager != null)
				{
					unitManager.SelectionHighlightState = state;
				}
			}
		}


		private bool AttackUnit(Vector3 mousePosition) 
		{
			if (AttackTags != null && selectedUnits.Count != 0)
			{			
				// Draw ray from camera toward pointed position, execute command only on hit (any tag)	
				Ray ray = cam.ScreenPointToRay(mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance))
				{
					// Verify if the hit gameobject tag is one that can be attacked
					bool anyAttacked = false;
					foreach (var tag in AttackTags)
					{
						if (hit.transform.CompareTag(tag) && hit.collider != null)
						{			
							// Apply newly pointed unit as target of already selected units
							GameObject target = hit.collider.gameObject;
							foreach (var unit in selectedUnits)
							{
								// Ask for the attack command if the selected/attacked units are not the same (cannot attack itself)
								if (target != unit) unit.GetComponent<UnitManager>().Attack(target);

								#if OUTPUT_DEBUG
								#region DEBUG
								if (unit != null && unit.GetComponent<UnitManager>() != null && target != null && target.GetComponent<UnitManager>() != null) 
								{
									string attackUnitName = unit.GetComponent<UnitManager>().Name;
									string hitName = target.GetComponent<UnitManager>().Name;
									Debug.Log(string.Format("Attack: {0}, {1} => {2}, {3}", unit.tag, attackUnitName, tag, hitName));
								}
								#endregion
								#endif
							}
							anyAttacked = true;
						}
					}	

					// If no unit was hit for an attack, cancel any existing attack command by removing the target reference
					if (!anyAttacked)
					{
						foreach (var unit in selectedUnits)
						{
							unit.GetComponent<UnitManager>().Attack(null);
						}
					}
					else return true;	// True if any unit was attacked
				}	
			}
			return false;	// False in any case except when any unit was attacked
		}


		private bool MoveUnit(Vector3 mousePosition)
		{




			return false;	// Return value in case of invalid movement
		}
	}
}
