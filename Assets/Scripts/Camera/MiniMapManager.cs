using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Camera))]
public class MiniMapManager : MonoBehaviour 
{
    public Terrain GroundTerrain;
    public float MapBorder = 0.01f;
    public float MapSize = 0.25f;


	void Start() 
    {
        SetMiniMapScreenRectangle();
	}
	
	
	void Update() 
    {
	
	}


    private void SetMiniMapScreenRectangle()
    {
        // Prepare map normalized viewport position and dimensions
        var minimap = GetComponent<Camera>();
        float border = MapBorder;
        float height = MapSize;
        float width = height / minimap.aspect;  // Adjust aspect ratio to get a square map

        // Apply normalized map rectangle
        minimap.rect = new Rect(border, 1 - border - height, width, height);

        // Apply terrain dimension to orthographic half-size to capture all of it on the map 
        minimap.orthographicSize = Mathf.Max(GroundTerrain.terrainData.size.x, GroundTerrain.terrainData.size.y) / 2;
    }
}
