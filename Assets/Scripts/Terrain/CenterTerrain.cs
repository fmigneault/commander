using UnityEngine;
using System.Collections;

public class CenterTerrain : MonoBehaviour 
{
	void Awake ()
	{
		Terrain t = GetComponent<Terrain>();
		Vector3 centered = transform.position;
		centered.x = -t.terrainData.size.x / 2;
		centered.y = 0;
		centered.z = -t.terrainData.size.z / 2;
		transform.position = centered;
	}
}
