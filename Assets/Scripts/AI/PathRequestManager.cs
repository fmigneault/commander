using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace AI
{
    // Spreads out the path requests over multiple frames to avoid locking / freezing the game upon multiple requests
    public class PathRequestManager : MonoBehaviour 
    {
    	Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    	PathRequest currentPathRequest;

    	static PathRequestManager instance;
    	Pathfinding pathfinding;
    	bool isProcessingPath;


        struct PathRequest 
        {
            public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
            {
                PathStart = start;
                PathEnd = end;
                Callback = callback;
            }


            public Vector3 PathStart { get; private set; }
            public Vector3 PathEnd { get; private set; }
            public Action<Vector3[], bool> Callback { get; private set; }
        }


    	void Awake() 
        {
    		instance = this;
    		pathfinding = GetComponent<Pathfinding>();
    	}


        // Function to request a new pathfinding request by outside code
    	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
        {
    		PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
    		instance.pathRequestQueue.Enqueue(newRequest);
    		instance.TryProcessNext();
    	}


    	void TryProcessNext() 
        {
    		if (!isProcessingPath && pathRequestQueue.Count > 0)
            {
    			currentPathRequest = pathRequestQueue.Dequeue();
    			isProcessingPath = true;
                pathfinding.StartFindPath(currentPathRequest.PathStart, currentPathRequest.PathEnd);
    		}
    	}


        // Callback the function passed to indicate to the unit that the path request has been processed
    	public void FinishedProcessingPath(Vector3[] path, bool success) 
        {
    		currentPathRequest.Callback(path,success);
    		isProcessingPath = false;
    		TryProcessNext();
    	}
    }
}