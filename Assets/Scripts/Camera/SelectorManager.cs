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
		public string ButtonTag;			// Tag of GameObjects corresponding to buttons

		// GUI Icon Panel
		public GameObject IconPanel;

		// Key mappings
		public KeyCode MultipleSelectKey = KeyCode.LeftControl;
		public KeyCode UnitSelectionKey = KeyCode.Mouse0;
		public KeyCode UnitAttackMoveKey = KeyCode.Mouse1;

		// Internal control flags, selected objects memory and control parameters
		private Camera cam;
		private float maxDistance;
		List<GameObject> selectedUnits;		// Currently selected units, accumulation possible if multiple key is used
		GameObject selectedBuilding;		// Currently selected building, only one allowed at a time
		private bool buttonClickedFlag;		// Flag enabled when a OnClick event has called 'ClickButtonPanel'


		void Start ()
		{
			cam = gameObject.GetComponent<RTS_Camera>().GetComponent<Camera>();
			maxDistance = gameObject.GetComponent<RTS_Camera>().maxHeight * 2;	
			selectedUnits = new List<GameObject>();
			ChangeIconPanelVisibility(IconPanel, false);
		}
		

		void Update () 
		{			
			Vector3 mousePosition = Input.mousePosition;

			if (Input.GetKeyUp(UnitSelectionKey))
			{			
				SelectObjects(GetPointedObject(mousePosition), Input.GetKey(MultipleSelectKey));
			}
			else if (Input.GetKeyUp(UnitAttackMoveKey))
			{
				bool attaking = AttackUnit(GetPointedObject(mousePosition));
				if (!attaking) MoveUnit(mousePosition);
			}
			buttonClickedFlag = false;	// Reset
		}


		public void ClickButtonPanel(Button buttonCliked)
		{
			// Update button clicked flag from external OnClick event (button must link to this function)
			//   If a button clicked event was used to call this function with it's corresponding object as parameter,
			//	 then enable the flag. Otherwise, disable the flag.
			buttonClickedFlag = (IconPanel != null && buttonCliked != null);
			if (!buttonCliked) return;

			// Find the clicked button index
			var btns = IconPanel.GetComponentsInChildren<Button>();
			int idx = btns.ToList().IndexOf(buttonCliked);

			#if OUTPUT_DEBUG
			#region DEBUG
			Debug.Log(string.Format("BUTTON CLICKED: {0}, {1}", buttonCliked.name, buttonClickedFlag));
			#endregion
			#endif	

			// Add corresponding unit to building production queue if a building was already selected
			// (clickable displayed icon in the panel would therefore be available units to produce with this building)
			if (selectedBuilding != null)
			{
				var buildingManager = selectedBuilding.GetComponent<BuildingManager>();
				buildingManager.AddUnitToProductionQueue(buildingManager.ProducedUnits[idx]);
			}
			// Generate a new instance of the corresponding building if a construction unit was selected
			else if (selectedUnits.Count == 1 && selectedUnits.First().tag == ConstructionTag)
			{
				var unitManager = selectedUnits.First().GetComponent<UnitManager>();
				var newBuilding = unitManager.ProducedBuildings[idx];
				newBuilding.GetComponent<BuildingPlacementManager>().PlaceNewBuilding();
			}
		}


		private GameObject GetPointedObject(Vector3 mousePosition)
		{
			// Draw ray from camera toward pointed position, the get collided object if available
			Ray ray = cam.ScreenPointToRay(mousePosition);
			RaycastHit hit;
			return Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance) ? hit.collider.gameObject : null;
		}


		private void SelectObjects(GameObject objectHit, bool multipleSelect=false)
		{
			// Verify that selectable unit tags exist in the GameObject and that no button was previously clicked
			if (SelectTags != null && !buttonClickedFlag)
			{		
				#if OUTPUT_DEBUG
				#region DEBUG
				Debug.Log(objectHit.tag);
				#endregion
				#endif	

				// Verify if the hit gameobject is one of the selectable unit and that no button was previously clicked
				bool anySelected = false;
				if (SelectTags.Contains(objectHit.tag))
				{								
					// If the selected object is a building, unselect units and display icons of produced units
					if (objectHit.tag == BuildingTag)
					{
						UnSelectAllUnits();
						selectedBuilding = objectHit;
						var iconList = objectHit.GetComponent<BuildingManager>().ProducedUnits;
						PopulateIconPanel(IconPanel, iconList);
						ChangeIconPanelVisibility(IconPanel, true);
						return;
					}
					else
					{
						selectedBuilding = null;
						ChangeIconPanelVisibility(IconPanel, false);
					}

					// If not using multiple selection, un-select previously selected units
					if (!multipleSelect) UnSelectAllUnits();						

					// If using multiple selection, un-select already selected unit
					if (multipleSelect && selectedUnits.Contains(objectHit))
					{
						UnselectSingleUnit(objectHit);
						return;
					}

					// Update unit selection
					SetUnitHighlightState(objectHit, true);
					selectedUnits.Add(objectHit);
					anySelected = (selectedUnits.Count > 0);

					// Update GUI building icons, display if only one "construction" unit is selected
					bool displayBuildings = (selectedUnits.Count == 1 && 
											 selectedUnits.ToArray()[0].tag == ConstructionTag);
					if (displayBuildings)
					{								
						var iconList = objectHit.GetComponent<UnitManager>().ProducedBuildings;
						PopulateIconPanel(IconPanel, iconList);
						ChangeIconPanelVisibility(IconPanel, displayBuildings);
					}

					#if OUTPUT_DEBUG
					#region DEBUG
					if (objectHit != null && objectHit.GetComponent<UnitManager>() != null) 
					{
						string hitName = objectHit.GetComponent<UnitManager>().Name;
						Debug.Log(string.Format("Selected: {0}, {1}", objectHit.tag, hitName));
					}
					#endregion
					#endif
				}

				// If no valid unit tag was hit, unselect units (ex: click on terrain)
				if (!anySelected) UnSelectAllUnits();	
			}
		}


		private bool AttackUnit(GameObject hitObject) 
		{
            if (hitObject != null && AttackTags != null && selectedUnits.Count != 0)
			{			
				// Get newly pointed unit selection
				#if OUTPUT_DEBUG
				#region DEBUG
				Debug.Log(hitObject.tag);
				#endregion
				#endif	

				// Verify if the hit gameobject tag is one that can be attacked
				bool anyAttacked = false;
				if (AttackTags.Contains(hitObject.tag))
				{			
					// Apply newly pointed unit as target of already selected units
					foreach (var unit in selectedUnits)
					{
						// Ask for the attack command if the selected/attacked units are not the same (cannot attack itself)
						if (hitObject != unit) unit.GetComponent<UnitManager>().Attack(hitObject);

						#if OUTPUT_DEBUG
						#region DEBUG
						if (unit != null && unit.GetComponent<UnitManager>() != null && hitObject != null && hitObject.GetComponent<UnitManager>() != null) 
						{
							string attackUnitName = unit.GetComponent<UnitManager>().Name;
							string hitName = hitObject.GetComponent<UnitManager>().Name;
							Debug.Log(string.Format("Attack: {0}, {1} => {2}, {3}", unit.tag, attackUnitName, hitObject.tag, hitName));
						}
						#endregion
						#endif
					}
					anyAttacked = true;
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
			return false;	// False in any case except when any unit was attacked
		}


		private bool MoveUnit(Vector3 mousePosition)
		{




			return false;	// Return value in case of invalid movement
		}

	
		private void UnSelectAllUnits()
		{
			foreach (var unit in selectedUnits)
			{
				SetUnitHighlightState(unit, false);
			}
			selectedUnits.Clear();
			ChangeIconPanelVisibility(IconPanel, false);
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
				var btns = panel.GetComponentsInChildren<Button>();
				for (int i = 0; i < btns.Length; i++)
				{					
					if (i < objectsWithIcon.Count)
					{
						// Set the button image, display it and associate the object link
						var obj = objectsWithIcon.ToArray()[i];
						var imgMan = obj.GetComponent<IconManager>();
						btns[i].image.overrideSprite = imgMan.Icon;
						btns[i].enabled = true;
						btns[i].image.color = Color.white;
					}
					else
					{	
						// Revert button image to default and hide it
						btns[i].image.overrideSprite = null;
						btns[i].enabled = false;
						btns[i].image.color = Color.clear;
					}
				}
			}
		}
	}
}
