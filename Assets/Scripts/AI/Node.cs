using UnityEngine;
using System.Collections;

namespace AI
{
    public class Node : IHeapItem<Node> 
    {
    	public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY) 
        {
    		Walkable = walkable;
    		WorldPosition = worldPosition;
    		GridX = gridX;
    		GridY = gridY;
    	}


        public int gCost { get; set; }                      // Distance from start node
        public int hCost { get; set; }                      // Distance from end node (heuristic)
        public int fCost { get { return gCost + hCost; } }  // Total cost (minimization)

        public bool Walkable { get; set; }
        public Vector3 WorldPosition { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int HeapIndex { get; set; }
        public Node Parent { get; set; }


    	public int CompareTo(Node nodeToCompare) 
        {
    		int compare = fCost.CompareTo(nodeToCompare.fCost);
    		if (compare == 0) 
            {
    			compare = hCost.CompareTo(nodeToCompare.hCost);
    		}
    		return -compare;
    	}
    }
}