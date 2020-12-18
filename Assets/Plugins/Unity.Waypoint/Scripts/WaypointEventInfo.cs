using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Waypoints
{
    [System.Serializable]
    public struct WaypointEventInfo
    {
        public string name;
        public float distance;
        public int intValue;
        public string stringValue;
        public float floatValue;
        public UnityEngine.Object objectValue;
        public TypeCode valueType;

        public object Value
        {
            get
            {
                switch (valueType)
                {
                    case TypeCode.String:
                        return stringValue;
                    case TypeCode.Int32:
                        return intValue;
                    case TypeCode.Single:
                        return floatValue;
                    case TypeCode.Object:
                        return objectValue;
                }
                return null;
            }
        }

    }

}