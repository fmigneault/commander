﻿// Display debugging/logging info on console
//#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS_Cam;
using Units;

namespace Buildings 
{	
	public class BuildingPlacementManager : MonoBehaviour 
    {
        
        public bool InPlacement = false;        // Flag that indicates if the building is currently being placed
        public Terrain GroundTerrain;           // Reference to terrain to align building with mouse position on ground
        public KeyCode BuildingMousePlaceKey = KeyCode.Mouse0;      // Mouse button to place at current position
        public KeyCode BuildingMouseRotationKey = KeyCode.Mouse1;   // Mouse button to rotate the buiding around itself            
        public List<string> PlacementCollisionTags = null;          // Collision tags considered for invalid placement

        // RTS Camera control and references
        private bool originalMouseRotationStatus;   // Allows reset of the original 'useMouseRotation' setting
        private RTS_Camera RTSCamera;

        // Color adjustment when placing building (warning: zero values make color information irreversible, to avoid)
        private Color validPlacementMultiplier   = new Color(1,2,1);    // Green multiplier for valid placement
        private Color invalidPlacementMultiplier = new Color(2,1,1);    // Red multiplier for invalid placement 
        private Color currentPlacementMultiplier = new Color(1,1,1);    // For resetting default multiplier

        // Counter indicating if the building can be placed down (when no collision)
        //    Use an integer to accumulate/decrement repectively to trigger enter/exit events
        //    This ensures to not allow building placement in case of multiple overlaps triggering in alternance
        private int overlappingBuildingCounter = 0;


        void Start()
        {
            RTSCamera = Camera.main.GetComponent<RTS_Camera>();
            originalMouseRotationStatus = RTSCamera.useMouseRotation;

            // Activate trigger events, this allows to detect new trigger events only when placing the building
            //    When trigger is pre-enabled within the Unity Editor, all building instances get triggers, which makes
            //    some of the collision detection fail to work as intented (triggers of already placed building launches
            //    a 'OnTriggerExit' which in turn makes this building 'valid' for placement although it overlaps)
            GetComponent<BoxCollider>().isTrigger = true;               
        }

		
		void Update () 
		{
			// Allow building placement management only if the flag is enabled
            if (InPlacement)
            {   
                bool rotate = Input.GetKey(BuildingMouseRotationKey);
                bool place = Input.GetKeyUp(BuildingMousePlaceKey);

                // Adjust building rotation while the mouse button is held down, otherwise adjust position
                if (rotate)     RotateBuildingTowardDirection();
                else if (place) PlaceDownBuilding();
                else            FollowMousePosition();

                // Display the directional arrow if rotating the building
                var buildingManager = gameObject.GetComponent<BuildingManager>();
                if (buildingManager != null) buildingManager.PointingArrowState = rotate;

                #if OUTPUT_DEBUG
                #region DEBUG
                if (RTSCamera != null) 
                {
                    Debug.Log(string.Format("PLACEMENT UPDATING, {0}", RTSCamera.useMouseRotation));
                }
                else
                {
                    Debug.Log("PLACEMENT UPDATING - NULL CAMERA");
                }
                #endregion
                #endif               
            }
		}
            

		public void RequestNewBuildingPlacement(GameObject constructionUnit)
		{			
            // A request to create a new building instance is done calling this function using the prefab, but the 
            // changes have to be applied to the instance of the building instanciated. Therefore, the instance is
            // created and following adjustment handling are passed to its corresponding 'BuildingPlacementManager'.
			var newBuilding = Instantiate(gameObject);
            var newBuildingPlaceMan = newBuilding.GetComponent<BuildingPlacementManager>();
            newBuildingPlaceMan.InPlacement = true;

            // Transfer the construction unit faction color to the created building
            var newBuildingManager = newBuilding.GetComponent<BuildingManager>();
            var constructionUnitManager = constructionUnit.GetComponent<UnitManager>();
            newBuildingManager.FactionColor = constructionUnitManager.FactionColor;

            #if OUTPUT_DEBUG
            #region DEBUG
            Debug.Log("PLACEMENT MANAGER CALLED");
            #endregion
            #endif
		}


        private void PlaceDownBuilding()
        {
            if (overlappingBuildingCounter == 0)
            {
                FollowMousePosition();
                InPlacement = false;
                RTSCamera.useMouseRotation = originalMouseRotationStatus;   // Reset the original camera rotation option
                ApplyColorMultiplierToBuilding(new Color(1, 1, 1));         // Reset original color

                // Desactivate trigger events
                //    Since the building is placed down, it will not move anymore. No need to detect future trigger
                //    event (enter/exit). Future collisions will be managed by following buildings that need placement.
                GetComponent<BoxCollider>().isTrigger = false;     

                // Reset global building placement flag
                RTSCamera.GetComponent<SelectorManager>().AnyInPlacementFlag = false;

                #if OUTPUT_DEBUG
                #region DEBUG
                Debug.Log(string.Format("PLACE BUILDING DOWN, Camera Rotation: {0}", RTSCamera.useMouseRotation));
                #endregion
                #endif
            }
        }


        private void FollowMousePosition() 
        {
            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos)) gameObject.transform.position = mousePos;
        }


        private void RotateBuildingTowardDirection()
        {          
            RTSCamera.useMouseRotation = false;     // Override to lock camera rotation while rotating the building

            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos))
            {                                
                transform.LookAt(mousePos);
            }               
        }
            

        private void ApplyColorMultiplierToBuilding(Color colorMultiplier)
        {     
            // Find all sub-parts of the building (they must all have their color values adjusted)
            var buildingParts = gameObject.GetComponentsInChildren<MeshRenderer>();

            // Get the inverse of the previous color multiplier to reset to default color (return to (1,1,1) multiplier)
            var invertMultiplier = new Color(1 / currentPlacementMultiplier.r, 
                                             1 / currentPlacementMultiplier.g, 
                                             1 / currentPlacementMultiplier.b);
                
            // Apply new multiplier to all parts and update current multiplier
            foreach (var part in buildingParts)
            {                   
                part.material.color *= invertMultiplier * colorMultiplier;
            }
            currentPlacementMultiplier = colorMultiplier;
        }


        private bool GetTerrainPositionFromMouse(out Vector3 mousePosition) 
        {            
            // Adjust the building position to mouse location on the terrain
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray);
            foreach (var h in hits)
            {
                // Find the terrain from all objects crossed by the raycast 
                //    Finding the terrain specifically ensures that the building position will be adjusted relatively 
                //    to the terrain. Otherwise, the building causes an occlusion of the terrain since it follows the
                //    mouse position, which returns an invalid distance and causes the building to jitter while placing
                if (h.transform.gameObject.tag == GroundTerrain.tag)
                {
                    mousePosition = ray.GetPoint(h.distance);
                    return true;
                }
            }
            mousePosition = new Vector3();
            return false;
        }


        void OnTriggerEnter(Collider otherCollider)
        {
            if (InPlacement && PlacementCollisionTags.Contains(otherCollider.gameObject.tag))
            {
                overlappingBuildingCounter++;
                ApplyColorMultiplierToBuilding(invalidPlacementMultiplier);
            }
        }


        void OnTriggerExit(Collider otherCollider)
        {            
            if (InPlacement && PlacementCollisionTags.Contains(otherCollider.gameObject.tag))
            {
                overlappingBuildingCounter--;
                if (overlappingBuildingCounter == 0) ApplyColorMultiplierToBuilding(validPlacementMultiplier);
            }
        }
	}
}
