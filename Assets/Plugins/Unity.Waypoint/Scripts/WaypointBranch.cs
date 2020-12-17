using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityEngine.Waypoints
{
    [Serializable]
    public struct WaypointBranch
    {
        public WaypointReference from;
        public WaypointReference to;
    }

}