using System;
using UnityEngine;

namespace AI
{
    public class UnitTracker : MonoBehaviour
    {
        private static int unitCounter = 0;


        public static int GetNewUnitID()
        {
            return ++unitCounter;
        }


        public static int InvalidID { get { return -1; } }
    }
}

