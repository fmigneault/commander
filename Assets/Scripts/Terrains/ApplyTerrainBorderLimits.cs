using UnityEngine;
using System.Collections;
using Cameras;

namespace Terrains
{
    [RequireComponent(typeof(RTS_CameraManager))]
    public class ApplyTerrainBorderLimits : MonoBehaviour 
    {
    	public GameObject terrain;
        private RTS_CameraManager cameraRTS;

    	void Start () 
    	{
            cameraRTS = gameObject.GetComponent<RTS_CameraManager>();	

    		// Camera limit set with ±limitX / limitY, so scaling has to be divided by 2
    		// Also, camera is moving in XY with Z downward, but terrain is oriented as XZ plane with Y upward
    		Terrain t = terrain.GetComponent<Terrain>();
            cameraRTS.limitX = t.terrainData.size.x / 2;
            cameraRTS.limitY = t.terrainData.size.z / 2;
    	}
    }
}