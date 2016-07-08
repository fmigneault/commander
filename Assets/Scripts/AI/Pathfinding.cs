using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace AI
{
    public class Pathfinding : MonoBehaviour 
    {	
    	PathRequestManager requestManager;
    	Grid grid;
    	

    	void Awake() 
        {
    		requestManager = GetComponent<PathRequestManager>();
    		grid = GetComponent<Grid>();
    	}
    	
    	
    	public void StartFindPath(Vector3 startPos, Vector3 targetPos) 
        {
    		StartCoroutine(FindPath(startPos,targetPos));
    	}
    	

        // A* Pathfinding Algorithm
    	IEnumerator FindPath(Vector3 startPos, Vector3 targetPos) 
        {
    		Vector3[] waypoints = new Vector3[0];   // Waypoints that will compose the path
    		bool pathSuccess = false;
    		
    		Node startNode = grid.NodeFromWorldPoint(startPos);
    		Node targetNode = grid.NodeFromWorldPoint(targetPos);

    		if (startNode.walkable && targetNode.walkable) 
            {
    			Heap<Node> openSet = new Heap<Node>(grid.MaxSize);      // Set of nodes to be evaluated
    			HashSet<Node> closedSet = new HashSet<Node>();          // Set of nodes already evaluated
    			openSet.Add(startNode);
    			
    			while (openSet.Count > 0)
                {
                    // First node in the heap is the one with the lowest F cost by default
    				Node currentNode = openSet.RemoveFirst();
    				closedSet.Add(currentNode);
    				
                    // Path found if current is the target, finish searching
    				if (currentNode == targetNode)
                    {
    					pathSuccess = true;
    					break;
    				}
    				
                    // Explore neighbor nodes to the current one
    				foreach (Node neighbour in grid.GetNeighbours(currentNode)) 
                    {
                        // Evaluate only un-evaluated and traversable nodes
    					if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
    					
                        // Update node costs if a better path is found comapred to the previously saved cost
    					int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
    					if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                        {
    						neighbour.gCost = newMovementCostToNeighbour;
    						neighbour.hCost = GetDistance(neighbour, targetNode);
    						neighbour.parent = currentNode;
    						
                            // Add neighbor to the open list so it doesn't get evaluated again
    						if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
    					}
    				}
    			}
    		}
    		yield return null;  // Wait for one frame before returning 

    		if (pathSuccess) waypoints = RetracePath(startNode, targetNode);
    		requestManager.FinishedProcessingPath(waypoints, pathSuccess);    	
    	}

    	
        // Retrace back the found path from the end target node up to the starting node
    	static Vector3[] RetracePath(Node startNode, Node endNode) 
        {
    		List<Node> path = new List<Node>();
    		Node currentNode = endNode;
    		
    		while (currentNode != startNode) 
            {
    			path.Add(currentNode);
    			currentNode = currentNode.parent;
    		}

            // Find key waypoints only where there is a direction change to avoid redundant points along a strait line
    		Vector3[] waypoints = SimplifyPath(path);
    		Array.Reverse(waypoints);
    		return waypoints;    		
    	}

    	
        // Removes collinear mid points to preserve only the to limit points forming a strait line
    	static Vector3[] SimplifyPath(List<Node> path) 
        {
    		List<Vector3> waypoints = new List<Vector3>();
    		Vector2 directionOld = Vector2.zero;

    		for (int i = 1; i < path.Count; i ++) 
            {
    			Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX,path[i-1].gridY - path[i].gridY);
    			if (directionNew != directionOld) 
                {
    				waypoints.Add(path[i].worldPosition);
    			}
    			directionOld = directionNew;
    		}
    		return waypoints.ToArray();
    	}

    	
        // Distance between two nodes using diagonal and linear grid movements
    	static int GetDistance(Node nodeA, Node nodeB)
        {
    		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
    		int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
    		
            // Diagonal movement cost = 14, Linear movement cost = 10
            //    Comes for 1 distance between 4-connected nodes and sqrt(2) ~= 1.4 for diagonal, both multiplied by 10
    		if (dstX > dstY) return 14*dstY + 10* (dstX-dstY);
    		return 14*dstX + 10 * (dstY-dstX);
    	}
    }
}