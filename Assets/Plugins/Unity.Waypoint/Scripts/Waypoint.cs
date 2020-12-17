using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Waypoints
{
    [System.Serializable]
    public class Waypoint
    {
        public string id = System.Guid.NewGuid().ToString("N");
        public Vector3 position;
        public List<WaypointEventInfo> events;
        public override string ToString()
        {
            return $"{position}";
        }
    }
}