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
	public class SelectorManager : MonoBehaviour 
	{
        // Tags are displayed using a custom editor that allows to select tags using the existing ones in a list
        // Hide the values displayed by default when public (but have to be public to allow accessing them)
        [HideInInspector]
		public string[] SelectTagsByPriority;   // Tags of GameObjects which selection is permitted
        [HideInInspector]                       // (priority sorted from most to least important)
        public string[] AttackTagsByPriority;   // Tags of GameObjects which unit being attacked is permitted
        [HideInInspector]                       // (priority sorted from most to least important)
        public string BuilderTag;		        // Tag of GameObjects which are builder units
        [HideInInspector]
        public string BuildingTag;			    // Tag of GameObjects which are selectable buildings
        [HideInInspector]
        public string ButtonTag;                // Tag corresponding to a UI button

		// GUI Elements
        public Canvas CanvasGUI;                // The overall canvas reference
		public GameObject IconPanel;            // Contains the icons to display the creatable units or buildings      
        public GameObject SelectionBox;         // Rectangle image for region selection of units

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
		List<GameObject> selectedUnits;         // Currently selected units, accumulation possible if multiple key used
		GameObject selectedBuilding;            // Currently selected building, only one allowed at a time		
        private Vector3 startDragMousePositon;  // Temporary position memorize on key down (for region selection)
        private bool buttonClickedFlag;         // Flag enabled when a OnClick event has called 'ClickButtonPanel'
        private bool previousMouseRotation;     // Flag indicating if mouse rotation was occuring on the previous frame


		void Start()
		{            
            // Get camera components and assert missing references
            cameraRTS = gameObject.GetComponent<RTS_CameraManager>();
			cam = cameraRTS.GetComponent<Camera>();
            if (cam == null || cameraRTS == null) 
            {
                throw new MissingComponentException("Missing 'Camera' and/or 'RTS_CameraManager' components");
            }

            // Parameter initialization
            maxDistance = cameraRTS.maxHeight * 10;
			selectedUnits = new List<GameObject>();
			ChangeIconPanelVisibility(IconPanel, false);
            SelectionBox.SetActive(false);
            AnyInPlacementFlag = false;
            previousMouseRotation = false;
		}
		

		void Update() 
		{			
			Vector3 mousePosition = Input.mousePosition;

            // Execute selection box procedures
            //   Obtain the starting position of the selection area on the first click
            //   Update the displayed region while mouse is dragged and maintaining key pressed
            //   Get the units within the selection region when the mouse key is released
            if (Input.GetKeyDown(UnitSelectionKey))
            {                
                startDragMousePositon = mousePosition;
            }
            else if (Input.GetKey(UnitSelectionKey) && mousePosition != startDragMousePositon)
            {
                DrawSelectionRegion();
            }
            else if (SelectionBox.activeSelf && Input.GetKeyUp(UnitSelectionKey))
            {
                SelectUnitsFromRegion(startDragMousePositon, mousePosition); 
            }
            // If no region selection was in progress, the following commands can be executed
            // This way, we avoid unselection of units with following commands that use the same mouse click
            else
            {
                // Apply unit or building commands
                if (Input.GetKeyUp(UnitSelectionKey))
                {			
                    SelectObject(mousePosition, Input.GetKey(MultipleSelectKey));
                }
                // Since the same mouse click for unit movement/attack, building placement rotation and camera rotation 
                // can cause interferances, we validate the statuses camera and building rotation to allow unit commands
                // Since the mouse 'up' event marks the end of the camera rotation, we have to check the previous frame
                // to allow commands, otherwise the end of the camera rotation triggers selected units to move/attack
                else if (!AnyInPlacementFlag && !previousMouseRotation && Input.GetKeyUp(UnitAttackMoveKey))
                {
                    MoveAndOrAttackUnit(mousePosition);
                }
            }

            // Unselect any previously selected unit that got destroyed
            //    Use reverse order for loop because we cannot remove object in list while iterating over it with a 
            //    foreach loop and removing objects in the ascending order moves later indexes down by one each time.
            for (int i = selectedUnits.Count - 1; i >= 0; i--)
            {
                if (selectedUnits[i].GetComponent<UnitManager>().Health < 0) UnselectSingleUnit(selectedUnits[i]);
            }

            // Update control values
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
			//    If a button clicked event was used to call this function with it's corresponding object as parameter,
			//	  then enable the flag. Otherwise, disable the flag.
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
			// Generate a new instance of the corresponding building if a builder unit was selected
			else if (selectedUnits.Count == 1 && selectedUnits.First().tag == BuilderTag)
			{
                // Get the requested building and generate the new instance
                var constructionUnit = selectedUnits.First();
                var unitManager = constructionUnit.GetComponent<UnitManager>();
				var newBuilding = unitManager.ProducedBuildings[idx];
                newBuilding.GetComponent<BuildingPlacementManager>().RequestNewBuildingPlacement(constructionUnit);
                AnyInPlacementFlag = true;
			}
		}


        private Vector3 GetTerrainPositionFromMousePosition(Vector3 mousePosition)
        {
            GameObject dummy;
            Vector3 terrainPosition;
            var priorityTerrain = new[] { GroundTerrain.tag };
            GetPointedObject(mousePosition, out dummy, out terrainPosition, priorityTerrain);
            return terrainPosition;
        }


        // Returns true if any object was hit, adjusts 'hitObject' and the 'terrainPosition' accordingly
        // If no specific object gets hit, adjust 'terrainPosition', return false and 'hitObject' is the terrain
        // If many objects are aligned or overlapping, the selection of the hit object is done by descending priority
        private bool GetPointedObject(Vector3 mousePosition, out GameObject hitObject, 
                                      out Vector3 terrainPosition, string[] tagPriority = null)
		{
            // Draw ray from camera toward pointed position
            var ray = cam.ScreenPointToRay(mousePosition);
            var hits = Physics.RaycastAll(ray, maxDistance);
            var hitPriority = new RaycastHit();
            bool success = false;

            // Obtain the hit object by tag verification
            if (hits != null && hits.Any())
            {
                if (tagPriority != null && tagPriority.Any())
                {
                    // Find the hit object by tag priority
                    var hitTags = hits.Select(h => h.collider.tag);
                    foreach (var t in tagPriority)
                    {
                        var tagMatches = hitTags.Where(ht => ht.Equals(t));
                        if (tagMatches.Any())
                        {
                            hitPriority = hits.First(hp => hp.collider.tag == tagMatches.First());
                            success = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Simply get the first hit if no priority tag list was specified
                    hitPriority = hits.First();
                    success = true;
                }
            }

            // If successful, get collided object and corresponding terrain position
            if (success)
            {
                terrainPosition = ray.GetPoint(hitPriority.distance);
                terrainPosition.y = 0;
                hitObject = hitPriority.collider != null ? hitPriority.collider.gameObject : null;
            }
            // If unsuccessful, assume out of terrain bounds and project the pointed position on the terrain plane
            else
            {
                var terrainPlane = new Plane(GroundTerrain.transform.up, GroundTerrain.transform.position);
                float distance;
                terrainPosition = terrainPlane.Raycast(ray, out distance) ? ray.GetPoint(distance) : Vector3.zero;
                hitObject = GroundTerrain.gameObject;
            }

            return success;
		}
            

		private void SelectObject(Vector3 mousePosition, bool multipleSelect=false)
		{
			// Verify that selectable unit tags exist in the GameObject and that no button was previously clicked
            if (SelectTagsByPriority != null && !buttonClickedFlag)
			{		
                Vector3 hitPosition;
                GameObject hitObject;
                GetPointedObject(mousePosition, out hitObject, out hitPosition, SelectTagsByPriority);

				#if OUTPUT_DEBUG
				#region DEBUG
                if (hitObject == null) Debug.Log("No hit!");
                else Debug.Log(string.Format("Object hit: {0}", hitObject.tag));
				#endregion
				#endif	

				// Verify if the hit gameobject is one of the selectable unit
				bool anySelected = false;
                if (hitObject != null && SelectTagsByPriority.Contains(hitObject.tag))
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
                    else if (hitObject.tag != ButtonTag)
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
                    SelectUnit(hitObject);
                    UpdateIconsBuilderUnitSelected();
                    anySelected = (selectedUnits.Any());

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
            GetPointedObject(mousePosition, out hitObject, out hitPosition, AttackTagsByPriority);

            bool anyCommand = false;
            if (hitObject != null && AttackTagsByPriority != null && selectedUnits.Count != 0)
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
                        else if (AttackTagsByPriority.Contains(hitObject.tag))
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


        private void DrawSelectionRegion()
        {           
            // Get the starting box position and the current mouse position to get the box dimensions
            var currentMousePosition = Input.mousePosition;
            var region = SelectionBox.GetComponent<RectTransform>();
            var startPosition = new Vector2(startDragMousePositon.x, startDragMousePositon.y);

            // Calculate the box dimensions, adjust the values according to signs to display properly
            //    Scaling according to screen width/height is required since the canvas scaler changes dynamically
            //    Since the position and size are always positive, we need to offset the box for reverse positions
            var differenceArea = currentMousePosition - startDragMousePositon;
            if (differenceArea.x < 0)
            {
                differenceArea.x = -differenceArea.x;
                startPosition.x -= differenceArea.x;
            }
            if (differenceArea.y < 0)
            {
                differenceArea.y = -differenceArea.y;
                startPosition.y -= differenceArea.y;
            }       
            differenceArea.x /= CanvasGUI.transform.localScale.x;
            differenceArea.y /= CanvasGUI.transform.localScale.y;
              
            // Update the transformation values and activate the selection box to show it on screen
            region.position = new Vector3(startPosition.x, startPosition.y, 0);
            region.sizeDelta = new Vector2(differenceArea.x, differenceArea.y);
            SelectionBox.SetActive(true);
        }


        private void SelectUnitsFromRegion(Vector3 mousePositionStart, Vector3 mousePositionEnd)
        {
            // Hide the selection box since this function call mean the region is not required anymore
            SelectionBox.SetActive(false);

            // Find the other two corners than the start/end, ensure that the corners are in a clockwise order
            //    Corner clockwise order: (Start -> Corner1 -> End -> Corner2)
            var mousePositionCorner1 = new Vector3(mousePositionStart.x, mousePositionEnd.y, 0);
            var mousePositionCorner2 = new Vector3(mousePositionEnd.x, mousePositionStart.y, 0);
            var towarScreen = Vector3.forward;
            if (PositionRelativeToVector(mousePositionCorner1, mousePositionStart, mousePositionEnd, towarScreen) < 1)
            {
                // Inverse extra corners if not positionned properly, so that we get the clockwise order required
                var tmp = mousePositionCorner1;
                mousePositionCorner1 = mousePositionCorner2;
                mousePositionCorner2 = tmp;
            }

            // Project the 4 corners of the selection region onto the terrain
            var terrainPositionStart = GetTerrainPositionFromMousePosition(mousePositionStart);
            var terrainPositionEnd = GetTerrainPositionFromMousePosition(mousePositionEnd);
            var terrainPositionCorner1 = GetTerrainPositionFromMousePosition(mousePositionCorner1);
            var terrainPositionCorner2 = GetTerrainPositionFromMousePosition(mousePositionCorner2);

            #if OUTPUT_DEBUG
            #region DEBUG
            Debug.Log(string.Format("Mouse   - Start: {0}, Corner1: {1}, Corner2: {2}, End: {3}", mousePositionStart, 
                                    mousePositionCorner1, mousePositionCorner2, mousePositionEnd));
            Debug.Log(string.Format("Terrain - Start: {0}, Corner1: {1}, Corner2: {2}, End: {3}", terrainPositionStart, 
                                    terrainPositionCorner1, terrainPositionCorner2, terrainPositionEnd));
            #endregion
            #endif

            // Find all the units with a tag that allows their selection
            var selectableUnitList = new List<GameObject>(); 
            foreach (var t in SelectTagsByPriority)
            {
                if (t != BuildingTag) selectableUnitList.AddRange(GameObject.FindGameObjectsWithTag(t));
            }               
                
            // Unselect all previously selected units if the multi-selection key isn't pressed while using selection box
            if (!Input.GetKey(MultipleSelectKey)) UnSelectAllUnits();

            // Keep only the units contained within the boudaries of the selection region (cummulative if multi-select) 
            foreach (var unit in selectableUnitList)
            {
                // Unit is within the 4 selection region corners projected on the terrain if its position is to the 
                // right of each border forming the trapezoid shape in a clockwise order for any camera orientation
                //    Border clockwise order: (Start -> Corner1 -> End -> Corner2)
                if (PositionRelativeToVector(unit.transform.position, terrainPositionStart, terrainPositionCorner1, 
                                             GroundTerrain.transform.up) >= 0 && 
                    PositionRelativeToVector(unit.transform.position, terrainPositionCorner1, terrainPositionEnd, 
                                             GroundTerrain.transform.up) >= 0 &&
                    PositionRelativeToVector(unit.transform.position, terrainPositionEnd, terrainPositionCorner2, 
                                             GroundTerrain.transform.up) >= 0 &&
                    PositionRelativeToVector(unit.transform.position, terrainPositionCorner2, terrainPositionStart, 
                                             GroundTerrain.transform.up) >= 0)
                {
                    SelectUnit(unit);
                }
            }   

            // Update icons if only a builder unit was selected by region
            UpdateIconsBuilderUnitSelected();

            #if OUTPUT_DEBUG
            #region DEBUG
            Debug.Log(string.Format("Start: {0}, End: {1}", terrainPositionStart, terrainPositionEnd));
            Debug.Log(string.Format("Selectable Count: {0}", selectableUnitList.Count));
            Debug.Log(string.Format("Selected Count: {0}", selectedUnits.Count));
            #endregion
            #endif
        }


        // Returns -1 if the position is left of the line, 1 if on the right, and 0 if collinear 
        private static int PositionRelativeToVector(Vector3 position, Vector3 lineStart, Vector3 lineEnd, Vector3 up)
        {
            var lineVector = lineEnd - lineStart;
            var pointVector = position - lineStart;
            var perpendicular = Vector3.Cross(lineVector, pointVector);
            var direction = Vector3.Dot(perpendicular, up);

            if (direction > 0) return 1;
            else if (direction < 0) return -1;
            else return 0;
        }


        private void UpdateIconsBuilderUnitSelected()
        {
            // Update GUI building icons, display if only one "builder" unit is selected
            bool displayBuildings = (selectedUnits.Count == 1 && selectedUnits.First().tag == BuilderTag);
            if (displayBuildings)
            {        
                var iconList = selectedUnits.First().GetComponent<UnitManager>().ProducedBuildings;
                PopulateIconPanel(IconPanel, iconList);
                ChangeIconPanelVisibility(IconPanel, displayBuildings);
            }
        }


        private void SelectUnit(GameObject unit)
        {
            if (unit != null && !selectedUnits.Contains(unit))
            {
                var unitManager = unit.GetComponent<UnitManager>();
                if (unitManager != null && unitManager.Health > 0)
                {
                    SetUnitHighlightState(unit, true);
                    selectedUnits.Add(unit);
                    if (unitManager.HealthBar != null) unitManager.HealthBar.ForceVisible = true;
                }
            }
        }

	
		private void UnSelectAllUnits()
		{
			foreach (var unit in selectedUnits)
			{
				SetUnitHighlightState(unit, false);
                var unitManager = unit.GetComponent<UnitManager>();
                if (unitManager != null && unitManager.HealthBar != null) unitManager.HealthBar.ForceVisible = false;
			}
			selectedUnits.Clear();
			ChangeIconPanelVisibility(IconPanel, false);
		}


		private void UnselectSingleUnit(GameObject unit) 
		{		
            if (unit != null)
            {
                selectedUnits.Remove(unit);
                SetUnitHighlightState(unit, false);

                var unitManager = unit.GetComponent<UnitManager>();
                if (unitManager != null && unitManager.HealthBar != null) unitManager.HealthBar.ForceVisible = false;
            }
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
