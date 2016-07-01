// Display debugging/logging info on console
//#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cameras;
using Units;

namespace Buildings 
{	
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
	public class BuildingPlacementManager : MonoBehaviour 
    {       
        public Terrain GroundTerrain;           // Reference to terrain to align building with mouse position on ground
        public List<string> PlacementCollisionTags = null;          // Collision tags considered for invalid placement
        public KeyCode BuildingPlacementCancelKey = KeyCode.Escape; // Keyboard button to cancel building placement
        public KeyCode BuildingMousePlaceKey = KeyCode.Mouse0;      // Mouse button to place at current position
        public KeyCode BuildingMouseRotationKey = KeyCode.Mouse1;   // Mouse button to rotate the buiding around itself   
        public KeyCode BuildingQuickRotationKey = KeyCode.R;        // Keyboard button to quickly rotate the building
        public float BuildingQuickRotationDegrees = 90;             // Angle for quick rotation of building

        // RTS Camera control and references
        private bool originalMouseRotationStatus;   // Allows reset of the original 'useMouseRotation' setting
        private RTS_CameraManager CameraRTS;

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
            CameraRTS = Camera.main.GetComponent<RTS_CameraManager>();
            originalMouseRotationStatus = CameraRTS.useMouseRotation;

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
                bool quickRotate = Input.GetKeyUp(BuildingQuickRotationKey);
                bool rotate      = Input.GetKey(BuildingMouseRotationKey);
                bool place       = Input.GetKeyUp(BuildingMousePlaceKey);
                bool cancel      = Input.GetKeyUp(BuildingPlacementCancelKey);

                // Adjust building rotation while the mouse button is held down, otherwise adjust position
                if (quickRotate) QuickRotateBuilding();
                else if (rotate) RotateBuildingTowardDirection();
                else if (place)  PlaceDownBuilding();
                else if (cancel) CancelBuildingPlacement();
                else             FollowMousePosition();

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
           

        // Flag that indicates if the building is currently being placed
        public bool InPlacement
        {
            get;
            set;
        }


		public void RequestNewBuildingPlacement(GameObject constructionUnit)
		{			
            // A request to create a new building instance is done ny calling this function using the prefab, but the 
            // changes have to be applied to the instance of the building instanciated. Therefore, the instance is
            // created and following adjustment handling are passed to its corresponding 'BuildingPlacementManager'.
			var newBuilding = Instantiate(gameObject);
            var newBuildingPlaceMan = newBuilding.GetComponent<BuildingPlacementManager>();
            newBuildingPlaceMan.InPlacement = true;

            // Transfer the construction unit faction color to the created building
            var newBuildingManager = newBuilding.GetComponent<BuildingManager>();
            var constructionUnitManager = constructionUnit.GetComponent<UnitManager>();
            newBuildingManager.FactionColor = constructionUnitManager.FactionColor;
            newBuildingManager.MiniMapVisibility = false; // Hidden on minimap until the building is placed down

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
                FollowMousePosition();  // Place at current mouse position
                InPlacement = false;    // Stop local placement flag
                CameraRTS.useMouseRotation = originalMouseRotationStatus;   // Reset the original camera rotation option
                ApplyColorMultiplierToBuilding(new Color(1, 1, 1));         // Reset original color

                // Activate display on minimap since the building will not exist on the terrain
                GetComponent<BuildingManager>().MiniMapVisibility = true;

                // Desactivate trigger events
                //    Since the building is placed down, it will not move anymore. No need to detect future trigger
                //    event (enter/exit). Future collisions will be managed by following buildings that need placement.
                GetComponent<BoxCollider>().isTrigger = false;     

                // Reset global building placement flag
                CameraRTS.GetComponent<SelectorManager>().AnyInPlacementFlag = false;

                #if OUTPUT_DEBUG
                #region DEBUG
                Debug.Log(string.Format("PLACE BUILDING DOWN, Camera Rotation: {0}", RTSCamera.useMouseRotation));
                #endregion
                #endif
            }
        }


        private void CancelBuildingPlacement()
        {            
            // Reset global building placement flag
            CameraRTS.GetComponent<SelectorManager>().AnyInPlacementFlag = false;

            // Destroy object instance
            Destroy(gameObject);
        }


        private void FollowMousePosition() 
        {
            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos)) gameObject.transform.position = mousePos;
        }


        private void RotateBuildingTowardDirection()
        {          
            CameraRTS.useMouseRotation = false;     // Override to lock camera rotation while rotating the building

            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos))
            {                                
                transform.LookAt(mousePos);
            }               
        }


        void QuickRotateBuilding()
        {
            transform.Rotate(transform.up * BuildingQuickRotationDegrees);
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


        private bool GetTerrainPositionFromMouse(out Vector3 mousePositionOnTerrain) 
        {            
            // Adjust the building position to mouse location projected on the terrain
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
                    mousePositionOnTerrain = ray.GetPoint(h.distance);
                    return true;
                }
            }
            mousePositionOnTerrain = Vector3.zero;
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
