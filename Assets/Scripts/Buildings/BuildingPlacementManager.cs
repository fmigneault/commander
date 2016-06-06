using UnityEngine;
using System.Collections;

namespace Buildings 
{
	public class BuildingPlacementManager : MonoBehaviour 
	{
		public Terrain terrain;			// The terrain to place building on
		public GameObject building;		// The building to place on the terrain

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

			}
		}
	}
}
