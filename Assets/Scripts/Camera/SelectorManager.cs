// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Buildings;
using RTS_Cam;
using Units;
using UI;

namespace RTS_Cam
{
	[RequireComponent(typeof(RTS_Camera))]
	public class SelectorManager : MonoBehaviour 
	{
		public string[] SelectTags;			// Tags of GameObjects which selection is permitted
		public string[] AttackTags;			// Tags of GameObjects which unit being attacked is permitted
		public string ConstructionTag;		// Tag of GameObjects which are construction units
		public string BuildingTag;			// Tag of GameObjects which are selectable buildings

		// GUI Icon Panel
		public GameObject IconPanel;

		// Key mappings
		public KeyCode MultipleSelectKey = KeyCode.LeftControl;
		public KeyCode UnitSelectionKey = KeyCode.Mouse0;
		public KeyCode UnitAttackMoveKey = KeyCode.Mouse1;

		private float maxDistance;
		private Camera cam;
		List<GameObject> selectedObjects;


		void Start ()
		{
			cam = gameObject.GetComponent<RTS_Camera>().GetComponent<Camera>();
			maxDistance = gameObject.GetComponent<RTS_Camera>().maxHeight * 2;	
			selectedObjects = new List<GameObject>();
			ChangeIconPanelVisibility(IconPanel, false);
		}
		

		void Update () 
		{			
			Vector3 mousePosition = Input.mousePosition;

			if (Input.GetKeyUp(UnitSelectionKey))
			{								
				SelectObjects(mousePosition, Input.GetKey(MultipleSelectKey));
			}
			else if (Input.GetKeyUp(UnitAttackMoveKey))
			{
				bool attaking = AttackUnit(mousePosition);
				if (!attaking) MoveUnit(mousePosition);
			}
		}


		private void SelectObjects(Vector3 mousePosition, bool multipleSelect=false)
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
							GameObject obj = hit.collider.gameObject;

							#if OUTPUT_DEBUG
							#region DEBUG
							Debug.Log(obj.tag);
							#endregion
							#endif

							// If the selected object is a building, unselect units and display icons of produced units
							if (obj.tag == BuildingTag)
							{
								UnSelectAllUnits();
								var iconList = obj.GetComponent<BuildingManager>().ProducedUnits;
								PopulateIconPanel(IconPanel, iconList);
								ChangeIconPanelVisibility(IconPanel, true);
								return;
							}
							else
							{
								ChangeIconPanelVisibility(IconPanel, false);
							}

							// If not using multiple selection, un-select previously selected units
							if (!multipleSelect) UnSelectAllUnits();						

							// If using multiple selection, un-select already selected unit
							if (multipleSelect && selectedObjects.Contains(obj))
							{
								UnselectSingleUnit(obj);
								return;
							}

							// Update unit selection
							SetUnitHighlightState(obj, true);
							selectedObjects.Add(obj);
							anySelected = (selectedObjects.Count > 0);

							// Update GUI building icons, display if only one "construction" unit is selected
							bool displayBuildings = (selectedObjects.Count == 1 && 
													 selectedObjects.ToArray()[0].tag == ConstructionTag);
							if (displayBuildings)
							{								
								var iconList = obj.GetComponent<UnitManager>().ProducedBuildings;
								PopulateIconPanel(IconPanel, iconList);
								ChangeIconPanelVisibility(IconPanel, displayBuildings);
							}

							#if OUTPUT_DEBUG
							#region DEBUG
							if (obj != null && obj.GetComponent<UnitManager>() != null) 
							{
								string hitName = obj.GetComponent<UnitManager>().Name;
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
			foreach (var unit in selectedObjects)
			{
				SetUnitHighlightState(unit, false);
			}
			selectedObjects.Clear();
			ChangeIconPanelVisibility(IconPanel, false);
		}


		private void UnselectSingleUnit(GameObject unit) 
		{			
			selectedObjects.Remove(unit);
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


		private static void ChangeIconPanelVisibility(GameObject panel, bool visible)
		{
			if (panel != null)
			{
				var cg = panel.GetComponent<CanvasGroup>();
				cg.alpha = visible ? 1 : 0;
				cg.interactable = visible;
			}
		}


		public static void PopulateIconPanel(GameObject panel, List<GameObject> objectsWithIcon)
		{
			if (panel != null && objectsWithIcon != null)
			{
				var icons = panel.GetComponentsInChildren<RawImage>();
				for (int i = 0; i < icons.Length; i++)
				{					
					if (i < objectsWithIcon.Count)
					{
						var obj = objectsWithIcon.ToArray()[i];
						var iman = obj.GetComponent<IconManager>();
						icons[i].texture = iman.Icon;
						icons[i].enabled = true;
					}
					else
					{	
						icons[i].enabled = false;
					}
				}
			}
		}


		private bool AttackUnit(Vector3 mousePosition) 
		{
			if (AttackTags != null && selectedObjects.Count != 0)
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
							foreach (var unit in selectedObjects)
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
						foreach (var unit in selectedObjects)
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
