// Display debugging/logging info on console
//#define OUTPUT_DEBUG

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Buildings;
using Units;
using UI;

namespace Cameras
{
	[RequireComponent(typeof(RTS_CameraManager))]
	public class SelectorManager : MonoBehaviour 
	{
		public string[] SelectTags;			// Tags of GameObjects which selection is permitted
		public string[] AttackTags;			// Tags of GameObjects which unit being attacked is permitted
		public string ConstructionTag;		// Tag of GameObjects which are construction units
		public string BuildingTag;			// Tag of GameObjects which are selectable buildings

		// GUI Icon Panel
		public GameObject IconPanel;

        // Terrain
        public Terrain GroundTerrain;

		// Key mappings
		public KeyCode MultipleSelectKey = KeyCode.LeftControl; // Multi-unit selection additional key
        public KeyCode UnitSelectionKey = KeyCode.Mouse0;       // Unit selection mouse button (single and multi units)
		public KeyCode UnitAttackMoveKey = KeyCode.Mouse1;      // Unit movement and attack command mouse button

		// Internal control flags, selected objects memory and control parameters
		private Camera cam;
        private RTS_CameraManager cameraRTS;
		private float maxDistance;
		List<GameObject> selectedUnits;		// Currently selected units, accumulation possible if multiple key is used
		GameObject selectedBuilding;		// Currently selected building, only one allowed at a time
		private bool buttonClickedFlag;		// Flag enabled when a OnClick event has called 'ClickButtonPanel'
        private bool previousMouseRotation; // Flag indicating if a mouse rotation was occuring on the previous frame


		void Start()
		{            
            cameraRTS = gameObject.GetComponent<RTS_CameraManager>();
			cam = cameraRTS.GetComponent<Camera>();
            maxDistance = cameraRTS.maxHeight * 10;
			selectedUnits = new List<GameObject>();
			ChangeIconPanelVisibility(IconPanel, false);
            AnyInPlacementFlag = false;
            previousMouseRotation = false;
		}
		

		void Update() 
		{			
			Vector3 mousePosition = Input.mousePosition;

            if (Input.GetKeyUp(UnitSelectionKey))
            {			
                SelectObjects(mousePosition, Input.GetKey(MultipleSelectKey));
            }
            // Since the same mouse click for unit movement/attack, building placement rotation and camera rotation 
            // can cause interferances, we validate the statuses camera and building rotation to allow unit commands
            //    Since the mouse 'up' event marks the end of the camera rotation, we have to check the previous frame
            //    to allow commands, otherwise the end of the camera rotation triggers selected unit move/attack command
            else if (!AnyInPlacementFlag && !previousMouseRotation && Input.GetKeyUp(UnitAttackMoveKey))
			{
                MoveAndOrAttackUnit(mousePosition);
			}
            previousMouseRotation = cameraRTS.IsRotatingWithMouse;  // Update for next frame
			buttonClickedFlag = false;	// Reset
		}


        // Flag for building being placed, reset by 'BuildingPlacementManager'
        public bool AnyInPlacementFlag
        {
            get;
            set;
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
			Debug.Log(string.Format("Button clicked: {0}, {1}", buttonCliked.name, buttonClickedFlag));
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
                // Get the requested building and generate the new instance
                var constructionUnit = selectedUnits.First();
                var unitManager = constructionUnit.GetComponent<UnitManager>();
				var newBuilding = unitManager.ProducedBuildings[idx];
                newBuilding.GetComponent<BuildingPlacementManager>().RequestNewBuildingPlacement(constructionUnit);
                AnyInPlacementFlag = true;
			}
		}


        private bool GetPointedObject(Vector3 mousePosition, out GameObject hitObject, out Vector3 terrainPosition)
		{
			// Draw ray from camera toward pointed position
            // If successful, get collided object and corresponding terrain position
			Ray ray = cam.ScreenPointToRay(mousePosition);
			RaycastHit hit;
            bool succes = Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance);           

            terrainPosition = ray.GetPoint(hit.distance);
            terrainPosition.y = 0;
            hitObject = hit.collider != null ? hit.collider.gameObject : null;
            return succes;
		}
            

		private void SelectObjects(Vector3 mousePosition, bool multipleSelect=false)
		{
			// Verify that selectable unit tags exist in the GameObject and that no button was previously clicked
			if (SelectTags != null && !buttonClickedFlag)
			{		
                Vector3 hitPosition;
                GameObject hitObject;
                GetPointedObject(mousePosition, out hitObject, out hitPosition);

				#if OUTPUT_DEBUG
				#region DEBUG
                if (hitObject == null) Debug.Log("No hit!");
                else Debug.Log(string.Format("Object hit: {0}", hitObject.tag));
				#endregion
				#endif	

				// Verify if the hit gameobject is one of the selectable unit and that no button was previously clicked
				bool anySelected = false;
                if (hitObject != null && SelectTags.Contains(hitObject.tag))
				{		
					// If the selected object is a building, unselect units and display icons of produced units
					if (hitObject.tag == BuildingTag)
					{	                        
                        // Display creatable units by the building only if it is not currently being placed 
                        //    Avoids populating and displaying the icon list when clicking to place down the building 
                        //    as it would necessarily select it since it is under the cursor and the same mouse button 
                        //    can be used for both the selection of objects and the placing the building down
                        // Also check a global placement flag
                        //    Since overlapping building while placing one and another is already placed can possibily
                        //    return any of the building reference when clicking on them, we cannot ensure 'hitObject'
                        //    is the one currently being placed. This solves a error observed with overlapping buildings
                        if (!hitObject.GetComponent<BuildingPlacementManager>().InPlacement && !AnyInPlacementFlag)
                        {
                            // Disable selection of possible previously selected building or units
                            UnSelectAllUnits();
                            UnselectBuilding();           

                            // Update icon and highlight display
                            SetBuildingHighlightState(hitObject, true);
                            var iconList = hitObject.GetComponent<BuildingManager>().ProducedUnits;
    						PopulateIconPanel(IconPanel, iconList);
    						ChangeIconPanelVisibility(IconPanel, true);

                            // Update currently selected building
                            selectedBuilding = hitObject; 
    						return;
                        }
					}
					else
					{
                        UnselectBuilding();
						ChangeIconPanelVisibility(IconPanel, false);
					}

					// If not using multiple selection, un-select previously selected units
					if (!multipleSelect) UnSelectAllUnits();						

					// If using multiple selection, un-select already selected unit
					if (multipleSelect && selectedUnits.Contains(hitObject))
					{
						UnselectSingleUnit(hitObject);
						return;
					}

					// Update unit selection
					SetUnitHighlightState(hitObject, true);
					selectedUnits.Add(hitObject);
					anySelected = (selectedUnits.Count > 0);

					// Update GUI building icons, display if only one "construction" unit is selected
					bool displayBuildings = (selectedUnits.Count == 1 && 
											 selectedUnits.ToArray()[0].tag == ConstructionTag);
					if (displayBuildings)
					{								
						var iconList = hitObject.GetComponent<UnitManager>().ProducedBuildings;
						PopulateIconPanel(IconPanel, iconList);
						ChangeIconPanelVisibility(IconPanel, displayBuildings);
					}

					#if OUTPUT_DEBUG
					#region DEBUG
					if (hitObject != null && hitObject.GetComponent<UnitManager>() != null) 
					{
						string hitName = hitObject.GetComponent<UnitManager>().Name;
						Debug.Log(string.Format("Selected: {0}, {1}", hitObject.tag, hitName));
					}
					#endregion
					#endif
				}

				// If no valid unit tag was hit, unselect units (ex: click on terrain)
                if (!anySelected)
                {
                    UnSelectAllUnits();	
                    UnselectBuilding();
                }
			}
		}


        private bool MoveAndOrAttackUnit(Vector3 mousePosition) 
		{
            Vector3 hitPosition;
            GameObject hitObject;
            GetPointedObject(mousePosition, out hitObject, out hitPosition);

            bool anyCommand = false;
            if (hitObject != null && AttackTags != null && selectedUnits.Count != 0)
			{			
				// Get newly pointed unit selection
				#if OUTPUT_DEBUG
				#region DEBUG
                Debug.Log(string.Format("Move/Attack command received (type: {0})", hitObject.tag));
				#endregion
				#endif	

                foreach (var unit in selectedUnits)
                {
                    var unitManager = unit.GetComponent<UnitManager>();
                    if (unitManager != null)
                    {                        
                        if (hitObject.tag == GroundTerrain.tag)
                        {                                
                            unitManager.MoveToDestination(hitPosition);
                            anyCommand = true;
                        }
                        else if (AttackTags.Contains(hitObject.tag))
                        {
                            unitManager.AttackTarget(hitObject);
                            anyCommand = true;
                        }
                    }

                    #if OUTPUT_DEBUG
                    #region DEBUG
                    if (unit != null && unitManager != null && hitObject != null && hitObject.GetComponent<UnitManager>() != null) 
                    {
                        string hitName = hitObject.GetComponent<UnitManager>().Name;
                        Debug.Log(string.Format("Attack: {0}, {1} => {2}, {3}", unit.tag, unitManager.Name, hitObject.tag, hitName));
                    }
                    #endregion
                    #endif
                }
			}	
            return anyCommand;	// False in any case except if at least one unit received move/attack commands
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
				var unitManager = unit.GetComponent<UnitManager>();
				if (unitManager != null)
				{
					unitManager.SelectionHighlightState = state;
				}
			}
		}


        private void UnselectBuilding() 
        {                       
            SetBuildingHighlightState(selectedBuilding, false);
            selectedBuilding = null;
        }


        private void SetBuildingHighlightState(GameObject building, bool state) 
        {
            if (building != null)
            {
                var buildingManager = building.GetComponent<BuildingManager>();
                if (buildingManager != null)
                {
                    buildingManager.SelectionHighlightState = state;
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
