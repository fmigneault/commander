using UnityEngine;
using System.Collections;
using RTS_Cam;

[RequireComponent(typeof(RTS_Camera))]
public class ApplyTerrainBorderLimits : MonoBehaviour 
{
	public GameObject terrain;
	private RTS_Camera RTScamera;

	void Start () 
	{
		RTScamera = gameObject.GetComponent<RTS_Camera>();	

		// Camera limit set with ±limitX / limitY, so scaling has to be divided by 2
		// Also, camera is moving in XY with Z downward, but terrain is oriented as XZ plane with Y upward
		Terrain t = terrain.GetComponent<Terrain>();
		RTScamera.limitX = t.terrainData.size.x / 2;
		RTScamera.limitY = t.terrainData.size.z / 2;
	}
}
