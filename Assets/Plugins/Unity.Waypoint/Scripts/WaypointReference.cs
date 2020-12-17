using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine.Waypoints
{

    [System.Serializable]
    public struct WaypointReference
    {
        public WaypointPath path;
        public string pointId;

        public WaypointReference(WaypointPath path, string pointId)
        {
            this.path = path;
            this.pointId = pointId;
        }
        public bool HasWaypoint
        {
            get
            {
                Waypoint point;
                return GetWaypoint(out point);
            }
        }
        public bool GetWaypoint(out Waypoint point)
        {
            if (!path || string.IsNullOrEmpty(pointId))
            {
                point = null;
                return false;
            }
            point = path.FindWaypointById(pointId);
            if (point != null)
                return true;
            return false;
        }

        public bool GetWaypointIndex(out int index)
        {
            if (!path || string.IsNullOrEmpty(pointId))
            {
                index = -1;
                return false;
            }
            index = path.FindWaypointIndexById(pointId);
            if (index != -1)
                return true;
            return false;
        }

        public bool GetRoutePoint(out RoutePoint routePoint)
        {
            int index;
            if (GetWaypointIndex(out index))
            {
                routePoint = path.GetRoutePoint(path.IndexToDistance(index));
                return true;
            }
            routePoint = new RoutePoint();
            return false;
        }

    }


}