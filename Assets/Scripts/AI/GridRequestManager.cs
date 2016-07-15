using System;
using UnityEngine;

namespace AI
{
    public class GridRequestManager : MonoBehaviour
    {
        
        static GridRequestManager instance;
        public Terrain GroundTerrain = null;

        Grid grid;


        void Awake()
        {
            instance = this;
            grid = GetComponent<Grid>();
            if (grid == null) throw new Exception("Grid reference required in the GameObject");
            if (GroundTerrain == null) throw new Exception("Terrain reference required in the GameObject");
        }


        public static void RequestGridAreaUpdate(Vector3[] corners, int objectID = default(int))
        {
            // Validate existing list of corners
            if (corners == null || corners.Length <= 0) return;

            // Validate that the corners are all within the terrain boundaries
            var terrainSize = new Vector2(instance.GroundTerrain.terrainData.size.x, 
                                          instance.GroundTerrain.terrainData.size.z);
            var terrainMinMax = new Vector2(terrainSize.x / 2, terrainSize.y / 2);
            foreach (var corner in corners)
            {
                if (corner.x > terrainMinMax.x || corner.x < -terrainMinMax.x ||
                    corner.z > terrainMinMax.y || corner.z < -terrainMinMax.y) return;
            }

            // Request the grid update
            instance.grid.StartGridAreaUpdate(corners, objectID);
        }
    }
}

