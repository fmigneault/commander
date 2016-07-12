using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    public class WaypointPath
    {
        public WaypointPath()
        {
            Clear();    // Required to set the index as invalid
        }


        public void NewRequest(Vector3 startPosition, Vector3 endPosition)
        {
            ControlledPathRequest(startPosition, endPosition);
        }


        public void MoveNext()
        {
            if (CompleteWaypointPath != null && !AtLastWaypoint) CurrentIndex++;
            if (CurrentIndex >= TotalCount) Clear();
        }


        public void Clear()
        {
            CompleteWaypointPath = null;
            CurrentIndex = -1;      // Set the index as invalid to avoid a erroneous check with zero value

            // In some cases, a previous request that has not yet finished being processed would override a clear
            // command as the request finally completes processing and is received. Therefore, adjust a flag to
            // indicate a controlled cancel path request (ignores the next one received if applicable).
            CancelPathRequest = true;
        }


        // Currently set waypoint index
        public int CurrentIndex { get; private set; }


        public int RemainingCount { get { return CompleteWaypointPath.Length - CurrentIndex; } }


        public int TotalCount { get { return CurrentIndex >= 0 ? CompleteWaypointPath.Length : CurrentIndex; } }


        public Vector3 CurrentWaypoint { get { return CompleteWaypointPath[CurrentIndex]; } }


        public Vector3 LastWaypoint { get { return CompleteWaypointPath.Last(); } }


        public Vector3 FirstWaypoint { get { return CompleteWaypointPath.First(); } }


        public Vector3[] RemainingWaypoints
        { 
            get { return Empty ? null : CompleteWaypointPath.Skip(CurrentIndex).ToArray(); }
        }


        public Vector3[] CompleteWaypointPath { get; private set; }


        public Vector3 this[int i] { get { return CompleteWaypointPath[i]; } }


        public bool AtFirstWaypoint { get { return (CurrentIndex == 0); } }


        public bool AtLastWaypoint { get { return (!Empty && RemainingCount <= 1); } }


        public bool Empty { get { return (CurrentIndex < 0); } }


        // One-Shot flag that will cancel updating the next received path request
        // Used to account for delays between a request, its reception, and new events conditiosn occuring in between
        // Used in conjunction with the controlled path request function
        public bool CancelPathRequest { get; set; }


        // Controlled path request call using the interlock one-shot flag 
        // This function should always be used instead of accessing directly the 'PathRequestManager'
        private void ControlledPathRequest(Vector3 startPosition, Vector3 endPosition)
        {
            CancelPathRequest = false;  // Reset flag since the following call is for intentional reception of new path
            PathRequestManager.RequestPath(startPosition, endPosition, OnPathFound);
        }


        // Callback method for when the path request has finished being processed 
        public void OnPathFound(Vector3[] newPath, bool pathSuccess)
        {
            // Update only if pathfinding was successful and not cancelled while the trajectory was being processed
            if (pathSuccess && !CancelPathRequest)
            {                     
                CompleteWaypointPath = newPath; // Update the found waypoints path
                CurrentIndex = 0;
            }
            else if (CancelPathRequest)
            {
                CancelPathRequest = false;      // Reset one-shot since the processed path was received and cancelled
            }
        }
    }
}
