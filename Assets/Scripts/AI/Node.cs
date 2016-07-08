using UnityEngine;
using System.Collections;

namespace AI
{
    public class Node : IHeapItem<Node> 
    {
    	public bool walkable;
    	public Vector3 worldPosition;
    	public int gridX;
    	public int gridY;

    	public Node parent;
    	int heapIndex;
    	

    	public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY) 
        {
    		walkable = _walkable;
    		worldPosition = _worldPos;
    		gridX = _gridX;
    		gridY = _gridY;
    	}


        public int gCost { get; set; }                      // Distance from start node
        public int hCost { get; set; }                      // Distance from end node (heuristic)
        public int fCost { get { return gCost + hCost; } }  // Total cost (minimization)


    	public int HeapIndex 
        {
    		get { return heapIndex; }
    		set { heapIndex = value; }
    	}


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