using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS_Cam;

namespace Buildings 
{	
	public class BuildingPlacementManager : MonoBehaviour 
    {
        // Flag that indicates if the building is currently being placed
        public bool InPlacement = false;      
        public Terrain GroundTerrain;
        public RTS_Camera RTSCamera;
        public KeyCode BuildingMouseRotationKey = KeyCode.Mouse1;

        // Current color multiplier applied to building (allows reset)
        private Color currentColorMultiplier = new Color(1,1,1);      
        private bool originalMouseRotationStatus;

        void Start()
        {
            originalMouseRotationStatus = RTSCamera.useMouseRotation;
            //ApplyColorMultiplierToBuilding(gameObject, new Color(3,0,0));
            //gameObject.transform.position = new Vector3(-20, gameObject.transform.position.y, -20);
        }

		
		void Update () 
		{
			// Allow building placement management only if the flag is enabled
            if (InPlacement)
            {   
                RTSCamera.useMouseRotation = false;     // Override

                // Adjust building rotation while the mouse button is held down, otherwise adjust position
                if (Input.GetKey(BuildingMouseRotationKey))
                {
                    RotateBuildingTowardDirection();
                }
                else
                {
                    FollowMousePosition();
                }


                Debug.Log("PLACEMENT UPDATING");

            }
            else
            {
                RTSCamera.useMouseRotation = originalMouseRotationStatus;   // Reset
            }
		}


		public void PlaceNewBuilding()
		{			
            // A request to create a new building instance is done calling this function using the prefab, but the 
            // changes have to be applied to the instance of the building instanciated. Therefore, the instance is
            // created and following adjustment handling are passed to its corresponding 'BuildingPlacementManager'.
			var newBuilding = Instantiate(gameObject);
            newBuilding.GetComponent<BuildingPlacementManager>().InPlacement = true;

            Debug.Log("PLACEMENT MANAGER CALLED");

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
