using UnityEngine;
using System.Collections;

namespace Buildings 
{	
	public class BuildingPlacementManager : MonoBehaviour 
	{
		public Terrain terrain;							// The terrain to place building on
		public GameObject building;						// The building to place on the terrain
		public float TransparencyPercentage = 0.5f;		// Alpha percentage to apply when placing the new building


		// Use this for initialization
		void Start () 
		{
			
		}
		
		// Update is called once per frame
		void Update () 
		{
			// Adjust the height of the moving building onto the terrain
			if (building != null && terrain != null)
			{
				Debug.Log("PLACEMENT UPDATING");
			}
		}


		public void PlaceNewBuilding()
		{
			Debug.Log("PLACEMENT MANAGER CALLED");


			building = Instantiate(gameObject);
            ApplyTransparencyToBuilding(building);
			building.transform.position = new Vector3(0, building.transform.position.y, 0);

		}


        private void ApplyTransparencyToBuilding(GameObject building)
        {            
            

            var buildingParts = building.GetComponentsInChildren<MeshRenderer>();
            foreach (var part in buildingParts)
            {        
                var colorAlpha = part.material.color;
                colorAlpha.a = TransparencyPercentage;
                part.material.color = colorAlpha;
            }
        }
	}
}
