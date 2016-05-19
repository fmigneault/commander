using UnityEngine;
using System.Collections;
using RTS_Cam;

[RequireComponent(typeof(RTS_Camera))]
public class ApplyTerrainLimits : MonoBehaviour {

	public GameObject terrain;
	private RTS_Camera camera;

	// Use this for initialization
	void Start () {
		camera = gameObject.GetComponent<RTS_Camera>();	

		// Camera limit set with ±limitX / limitY, so scaling has to be divided by 2
		// Also, camera is moving in XY with Z downward, but terrain is oriented as XZ plane with Y upward
		camera.limitX = terrain.transform.localScale.x / 2;
		camera.limitY = terrain.transform.localScale.z / 2;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
