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
			if (Input.GetMouseButtonUp(0))
			{
				SelectUnits();
			}
			else if (Input.GetMouseButtonUp(1))
			{
				AttackUnit();
			}
		}


		private void SelectUnits()
		{
			if (SelectTags != null)
			{				
				Ray ray = cam.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance))
				{
					bool anySelected = false;
					foreach (var tag in SelectTags)
					{
						if (hit.transform.CompareTag(tag) && hit.collider != null)
						{							
							GameObject unit = hit.collider.gameObject;
							SetUnitHighlight(unit);
							selectedUnits.Add(unit);
							anySelected = true;

							#region DEBUG
							if (unit != null && unit.GetComponent<UnitManager>() != null) 
							{
								string hitName = unit.GetComponent<UnitManager>().Name;
								Debug.Log(string.Format("Selected: {0}, {1}", tag, hitName));
							}
							#endregion
						}

						if (!anySelected) UnSelectUnits();
					}					
				}	
			}
		}

	
		private void UnSelectUnits()
		{
			foreach (var unit in selectedUnits)
			{
				ResetUnitHighlight(unit);
			}
			selectedUnits.Clear();
		}


		private void SetUnitHighlight(GameObject unit) 
		{

		}


		private void ResetUnitHighlight(GameObject unit) 
		{

		}


		private void AttackUnit() 
		{
			if (AttackTags != null && selectedUnits.Count != 0)
			{				
				Ray ray = cam.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance))
				{
					bool anyAttacked = false;
					foreach (var tag in AttackTags)
					{
						if (hit.transform.CompareTag(tag) && hit.collider != null)
						{								
							GameObject target = hit.collider.gameObject;
							foreach (var unit in selectedUnits)
							{
								// Ask for the attack command if the selected/attacked units are not the same (cannot attack itself)
								if (target != unit) unit.GetComponent<UnitManager>().Attack(target);

								#region DEBUG
								if (unit != null && unit.GetComponent<UnitManager>() != null && target != null && target.GetComponent<UnitManager>() != null) 
								{
									string attackUnitName = unit.GetComponent<UnitManager>().Name;
									string hitName = target.GetComponent<UnitManager>().Name;
									Debug.Log(string.Format("Attack: {0}, {1} => {2}, {3}", unit.tag, attackUnitName, tag, hitName));
								}
								#endregion
							}
							anyAttacked = true;
						}
					}	
					if (!anyAttacked)
					{
						foreach (var unit in selectedUnits)
						{
							unit.GetComponent<UnitManager>().Attack(null);
						}
					}
				}	
			}
		}
	}
}
