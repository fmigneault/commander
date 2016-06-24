// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS_Cam;
using Units;

namespace Buildings 
{	
	public class BuildingPlacementManager : MonoBehaviour 
    {
        // Flag that indicates if the building is currently being placed
        public bool InPlacement = false;
        public Terrain GroundTerrain;
        public KeyCode BuildingMousePlaceKey = KeyCode.Mouse0;
        public KeyCode BuildingMouseRotationKey = KeyCode.Mouse1;

        // Current color multiplier applied to building (allows reset)
        private Color currentColorMultiplier = new Color(1,1,1);      

        // RTS Camera control and references
        private bool originalMouseRotationStatus;
        private RTS_Camera RTSCamera;


        void Start()
        {
            RTSCamera = Camera.main.GetComponent<RTS_Camera>();
            originalMouseRotationStatus = RTSCamera.useMouseRotation;

            //ApplyColorMultiplierToBuilding(gameObject, new Color(3,0,0));
            //gameObject.transform.position = new Vector3(-20, gameObject.transform.position.y, -20);
        }

		
		void Update () 
		{
			// Allow building placement management only if the flag is enabled
            if (InPlacement)
            {   
                // Adjust building rotation while the mouse button is held down, otherwise adjust position
                if (Input.GetKey(BuildingMouseRotationKey))
                {                    
                    RotateBuildingTowardDirection();
                }
                else if (Input.GetKeyUp(BuildingMousePlaceKey))
                {                    
                    PlaceDownBuilding();
                }
                else
                {
                    FollowMousePosition();
                }


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
            FollowMousePosition();
            InPlacement = false;
            RTSCamera.useMouseRotation = originalMouseRotationStatus;   // Reset the original camera rotation option

            #if OUTPUT_DEBUG
            #region DEBUG
            Debug.Log(string.Format("PLACE BUILDING DOWN, Camera Rotation: {0}", RTSCamera.useMouseRotation));
            #endregion
            #endif
        }


        private void ApplyColorMultiplierToBuilding(GameObject building, Color colorMultiplier)
        {
            currentColorMultiplier *= colorMultiplier;
            var buildingParts = building.GetComponentsInChildren<MeshRenderer>();
            foreach (var part in buildingParts)
            {                                        
                part.material.color *= currentColorMultiplier;
            }
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


        private void RotateBuildingTowardDirection()
        {          
            RTSCamera.useMouseRotation = false;     // Override to lock camera rotation while rotating the building

            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos)) transform.LookAt(mousePos);
        }


        private void FollowMousePosition() 
        {
            Vector3 mousePos;
            if (GetTerrainPositionFromMouse(out mousePos)) gameObject.transform.position = mousePos;
        }
	}
}
