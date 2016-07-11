// Output performance testing
#define OUTPUT_TIMER

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace AI
{
    // Spreads out the pathfinding calculations over multiple frames to avoid locking / freezing 
    // the game upon multiple requests coming from any number of units
    public class PathRequestManager : MonoBehaviour 
    {
    	Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    	PathRequest currentPathRequest;

    	static PathRequestManager instance;
    	Pathfinding pathfinding;
    	bool isProcessingPath;

        #region TEST TIMER
        #if OUTPUT_TIMER
        private Stopwatch timer;    // For testing algorithm performances
        #endif
        #endregion


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

            #region TEST TIMER
            #if OUTPUT_TIMER
            timer = new Stopwatch();
            #endif
            #endregion
    	}


        // Function to request a new pathfinding request from outside code
    	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
        {
            var newRequest = new PathRequest(pathStart, pathEnd, callback);
    		instance.pathRequestQueue.Enqueue(newRequest);
    		instance.TryProcessNext();
    	}


        // Tries to process the next available request in the queue
    	void TryProcessNext() 
        {
            if (!isProcessingPath && instance.pathRequestQueue.Count > 0)
            {
                currentPathRequest = instance.pathRequestQueue.Dequeue();
    			isProcessingPath = true;

                #region TEST TIMER
                #if OUTPUT_TIMER
                timer.Start();
                #endif
                #endregion
                
                pathfinding.StartFindPath(currentPathRequest.PathStart, currentPathRequest.PathEnd);
    		}
    	}


        // Callback the function passed to indicate to the unit that the path request has been processed
    	public void FinishedProcessingPath(Vector3[] path, bool success) 
        {
            #region TEST TIMER
            #if OUTPUT_TIMER
            timer.Stop();
            UnityEngine.Debug.Log(string.Format("Elapsed time: {0} ms", timer.ElapsedMilliseconds));
            timer.Reset();
            #endif
            #endregion

    		currentPathRequest.Callback(path, success);
    		isProcessingPath = false;
    		TryProcessNext();
    	}
    }
}