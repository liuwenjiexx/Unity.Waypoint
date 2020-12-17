using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Waypoints
{
    [System.Serializable]
    public struct WaypointEventInfo
    {
        public float distance;
        public string Function;
        public int intValue;
        public string stringValue;
        public float floatValue;
        public System.TypeCode valueType;
    }

}