using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AI
{
    public class Grid : MonoBehaviour 
    {
        // Parameter settings
    	public bool DisplayGridGizmos;
    	public LayerMask UnwalkableMask;    
    	public Terrain GroundTerrain;       // Reference to the terrain to generate nodes over its full area
    	public float NodeRadius;            // World positions precision according to Nodes size
    	
        private Node[,] grid;
        private Vector2 gridWorldSize;

    	float nodeDiameter;
    	int gridSizeX, gridSizeY;

    	void Awake() 
        {
    		nodeDiameter = NodeRadius*2;
            gridWorldSize = new Vector2(GroundTerrain.terrainData.size.x, GroundTerrain.terrainData.size.z);
    		gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
    		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
    		CreateGrid();
    	}

    	public int MaxSize 
        {
    		get { return gridSizeX * gridSizeY; }
    	}


        public Node this[int x, int y] 
        { 
            get { return grid[x,y]; }
            set { grid[x,y] = value; }
        }


    	void CreateGrid() 
        {
            
    		grid = new Node[gridSizeX,gridSizeY];
            Vector3 worldBottomLeft = GroundTerrain.transform.position + 
                                      Vector3.left * gridWorldSize.x / 2 + 
                                      Vector3.back * gridWorldSize.y / 2;

    		for (int x = 0; x < gridSizeX; x++) 
            {
    			for (int y = 0; y < gridSizeY; y++) 
                {
    				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + NodeRadius) +
                                         Vector3.forward * (y * nodeDiameter + NodeRadius);
    				bool walkable = !(Physics.CheckSphere(worldPoint, NodeRadius, UnwalkableMask));
    				grid[x,y] = new Node(walkable, worldPoint, x, y);
    			}
    		}
    	}


        public List<Node> GetNeighbors(Node node) 
        {
    		List<Node> neighbors = new List<Node>();

    		for (int x = -1; x <= 1; x++) 
            {
    			for (int y = -1; y <= 1; y++) 
                {
    				if (x == 0 && y == 0)
    					continue;

    				int checkX = node.GridX + x;
    				int checkY = node.GridY + y;

    				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) 
                    {
    					neighbors.Add(grid[checkX,checkY]);
    				}
    			}
    		}
    		return neighbors;
    	}
    	

    	public Node NodeFromWorldPoint(Vector3 worldPosition) 
        {
    		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
    		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
    		percentX = Mathf.Clamp01(percentX);
    		percentY = Mathf.Clamp01(percentY);

    		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
    		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
    		return grid[x,y];
    	}
    	

    	void OnDrawGizmos() 
        {
    		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));
    		if (grid != null && DisplayGridGizmos) 
            {
    			foreach (Node n in grid) 
                {
    				Gizmos.color = (n.Walkable) ? Color.white : Color.red;
    				Gizmos.DrawCube(n.WorldPosition, Vector3.one * (nodeDiameter-.1f));
    			}
    		}
    	}
    }
}