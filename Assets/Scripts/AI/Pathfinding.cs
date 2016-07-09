using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace AI
{
    public enum AlgorithmType
    {
        A_STAR,                 // Basic pathfinding implementation
        A_STAR_SQUARED_COST,    // Uses other distance cost calculation method, but produce the same output result
        THETA_STAR              // Adds line-of-sight evaluation to allow any-angle smoother path trajectories
    };


    public class Pathfinding : MonoBehaviour 
    {
        public AlgorithmType Algorithm = AlgorithmType.A_STAR;

    	private PathRequestManager requestManager;
    	private Grid grid;


    	void Awake() 
        {
    		requestManager = GetComponent<PathRequestManager>();
    		grid = GetComponent<Grid>();
    	}
    	
    	
    	public void StartFindPath(Vector3 startPos, Vector3 targetPos) 
        {
    		StartCoroutine(FindPath(startPos, targetPos));
    	}
    	

        // A* Pathfinding Algorithm
    	IEnumerator FindPath(Vector3 startPos, Vector3 targetPos) 
        {
    		var waypoints = new Vector3[0]; // Waypoints that will compose the final reduced path the unit must follow
    		bool pathSuccess = false;
    		
    		Node startNode = grid.NodeFromWorldPoint(startPos);
    		Node targetNode = grid.NodeFromWorldPoint(targetPos);

    		if (startNode.Walkable && targetNode.Walkable) 
            {
    			var openSet = new Heap<Node>(grid.MaxSize);     // Set of nodes to be evaluated
    			var closedSet = new HashSet<Node>();            // Set of nodes already evaluated
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
    				foreach (Node neighbor in grid.GetNeighbors(currentNode)) 
                    {
                        // Evaluate only un-evaluated and traversable nodes
    					if (!neighbor.Walkable || closedSet.Contains(neighbor)) continue;

                        // Path 2: Employed only by Theta*, allows any-angle for smoother paths 
                        //    Instead of limiting to 8-connected neighbors, therefore imposing 45 degree angles, the 
                        //    parents of the neighbors and the current node are used to look at higher distances as 
                        //    long as the evaluated distance remains in line-of-sight (ie: doesn't traverse unwalkable)
                        if (Algorithm == AlgorithmType.THETA_STAR && currentNode.Parent != null && 
                            InLineOfSight(currentNode.Parent, neighbor))
                        {       
                            int gCostParent = currentNode.Parent.gCost * currentNode.Parent.gCost;
                            int gCostNeighbor = neighbor.gCost * neighbor.gCost;
                            int moveCostNeighborParent = gCostParent + NodeDistance(currentNode.Parent, neighbor);
                            if (moveCostNeighborParent < gCostNeighbor || !openSet.Contains(neighbor))
                            {
                                // Update node costs if a better path is found using the parent nodes
                                neighbor.gCost = currentNode.Parent.gCost + NodeDistance(currentNode.Parent, neighbor);
                                neighbor.hCost = NodeDistance(neighbor, targetNode);
                                neighbor.Parent = currentNode.Parent;

                                // Add neighbor to the open list so it doesn't get evaluated again
                                if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                            }   
                        }
                        // Path 1 (default): Employed by both A* and Theta* (if not in line-of-sight)
                        else
                        {
                            // Cost calculation must be adapted according to the distance method
                            //    If using the local 8-connected neighbors, we simply apply the stardard formulat
                            //    If using the squared euclidian distance to avoid float values, gCost must be squared
                            int gCostNeighbor = 0;
                            int gCostCurrent = 0;
                            if (Algorithm == AlgorithmType.A_STAR)
                            {
                                gCostCurrent = currentNode.gCost;
                                gCostNeighbor = neighbor.gCost;
                            }
                            else if (Algorithm == AlgorithmType.A_STAR_SQUARED_COST ||
                                     Algorithm == AlgorithmType.THETA_STAR)
                            {
                                gCostCurrent = currentNode.gCost * currentNode.gCost;
                                gCostNeighbor = neighbor.gCost * neighbor.gCost;
                            }
                            int moveCostNeighbor = gCostCurrent + NodeDistance(currentNode, neighbor);
                            if (moveCostNeighbor < gCostNeighbor || !openSet.Contains(neighbor))
                            {
                                // Update node costs if a better path is found comapred to the previously saved cost
                                neighbor.gCost = currentNode.gCost + NodeDistance(currentNode, neighbor);
                                neighbor.hCost = NodeDistance(neighbor, targetNode);
                                neighbor.Parent = currentNode;

                                // Add neighbor to the open list so it doesn't get evaluated again
                                if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                            }   
                        }
    				}
    			}
    		}
    		yield return null;  // Wait for one frame before returning 

            if (pathSuccess) waypoints = RetracePath(startNode, targetNode);
    		requestManager.FinishedProcessingPath(waypoints, pathSuccess);    	
    	}

    	
        // Retrace back the found path from the end target node up to the starting node
    	Vector3[] RetracePath(Node startNode, Node endNode) 
        {
    		var path = new List<Node>();
    		Node currentNode = endNode;
    		
    		while (currentNode != startNode) 
            {
    			path.Add(currentNode);
    			currentNode = currentNode.Parent;
    		}
                
            // Reverse the order of the found waypoints going from destination to current starting position
            Vector3[] waypoints = GetPathWaypointsFromNodes(path);
            Array.Reverse(waypoints);
    		return waypoints;    		
    	}


        Vector3[] GetPathWaypointsFromNodes(List<Node> nodePath)
        {
            // Find key waypoints only where there is a direction change to avoid redundant points along a strait line
            //    Only required when using A* since Theta* automatically does this using the in line-of-sight check
            Vector3[] waypoints = null;
            if (Algorithm == AlgorithmType.A_STAR || Algorithm == AlgorithmType.A_STAR_SQUARED_COST)
            {
                waypoints = SimplifyPath(nodePath);
            }
            else if (Algorithm == AlgorithmType.THETA_STAR)
            {
                waypoints = new Vector3[nodePath.Count];
                for (int i = 0; i < nodePath.Count; i++) waypoints[i] = nodePath[i].WorldPosition;
            }
            return waypoints;
        }

    	
        // Removes collinear mid points to preserve only the to limit points forming a strait line
    	static Vector3[] SimplifyPath(IList<Node> path) 
        {
    		var waypoints = new List<Vector3>();
    		var directionOld = Vector2.zero;

    		for (int i = 1; i < path.Count; i++) 
            {
    			var directionNew = new Vector2(path[i-1].GridX - path[i].GridX, path[i-1].GridY - path[i].GridY);
    			if (directionNew != directionOld) 
                {
    				waypoints.Add(path[i].WorldPosition);
    			}
    			directionOld = directionNew;
    		}
    		return waypoints.ToArray();
    	}


        // Verifies if two nodes can linearly see each other without passing over a non traversable node
        bool InLineOfSight(Node nodeA, Node nodeB)
        {            
            int x0 = nodeA.GridX;
            int y0 = nodeA.GridY;
            int x1 = nodeB.GridX;
            int y1 = nodeB.GridY;
            int dx = x1 - x0;
            int dy = y1 - y0;
            int sx = 1;
            int sy = 1;
            if (dx < 0)
            {
                dx = -dx;
                sx = -1;
            }
            if (dy < 0)
            {
                dy = -dy;
                sy = -1;
            }
            int ssx = (sx - 1) / 2;
            int ssy = (sy - 1) / 2;

            int f = 0;
            if (dx >= dy)
            {
                while (x0 != x1)
                {
                    f += dy;
                    if (f >= dx)
                    {                        
                        if (!grid[x0 + ssx, y0 + ssy].Walkable) return false;
                        y0 += sy;
                        f -= dx;
                    }
                    if (f != 0 && !grid[x0 + ssx, y0 + ssy].Walkable) return false;
                    if (dy == 0 && !grid[x0 + ssx, y0].Walkable && !grid[x0 + ssx, y0 - 1].Walkable) return false;
                    x0 += sx;
                }
            }
            else
            {
                while (y0 != y1)
                {
                    f += dx;
                    if (f >= dy)
                    {
                        if (!grid[x0 + ssx, y0 + ssy].Walkable) return false;
                        x0 += sx;
                        f -= dy;
                    }
                    if (f != 0 && !grid[x0 + ssx, y0 + ssy].Walkable) return false;
                    if (dx == 0 && !grid[x0, y0 + ssy].Walkable && !grid[x0 - 1, y0 + ssy].Walkable) return false;
                    y0 += sy;
                }
            }

            // If this point is reached, the nodes can see each other linearly (in line-of-sight)
            return true;
        }


        // Distance between two nodes using diagonal and linear grid movements
        int NodeDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

            if (Algorithm == AlgorithmType.A_STAR)
            {
                // Diagonal movement cost = 14, Linear movement cost = 10
                //    Comes for 1 distance between 4-connected nodes and sqrt(2) ~= 1.4 for diagonal, multiplied by 1
                if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
                return 14 * dstX + 10 * (dstY - dstX);
            }
            else if (Algorithm == AlgorithmType.A_STAR_SQUARED_COST || Algorithm == AlgorithmType.THETA_STAR)
            {
                // Use the square of the euclidian distance
                //    Avoid using the more costly square root formula, and also avoids the use of float values
                return dstX * dstX + dstY * dstY;
            }

            return -1;  // In case of unsupported algorithm type (compiler requires return on all paths)
        }
    }
}