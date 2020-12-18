using System;
using System.Collections;
using System.Collections.Generic;



namespace UnityEngine.Waypoints
{
    public class WaypointTracker : MonoBehaviour
    {
        public WaypointPath path;
        [System.NonSerialized]
        public float pointToPointThreshold = 1f;
        public Transform target;
        public float branchDistanceThreshold = 5f;
        public Dictionary<WaypointPath, PathState> branchs = new Dictionary<WaypointPath, PathState>();
        [System.NonSerialized]
        public PathState mainPathState;
        [System.NonSerialized]
        public PathState actived;
        public bool directionMode;

        public static WaypointTracker Current { get; private set; }
        public static WaypointEventInfo CurrentEvent { get; private set; }
        public static List<WaypointTracker> List { get; private set; }

        [System.Serializable]
        public class PathState
        {
            public WaypointPath path;
            public float prevProgressDistance;
            public float progressDistance;
            public RoutePoint progressPoint;
            public int prevProgressIndex;
            public int progressIndex;
            public bool isActive;
            public float distance;
            public bool isMain;
            public List<int> processEvents = new List<int>();
            public List<int> processPoints = new List<int>();
        }


        public float Progress
        {
            get
            {
                float dist = mainPathState.progressDistance;
                if (path.closed)
                {
                    dist = mainPathState.progressDistance % path.Length;
                }

                return dist / path.Length;
            }
        }
        private void Awake()
        {
            if (List == null)
                List = new List<WaypointTracker>();
            List.Add(this);
        }

        // Use this for initialization
        void Start()
        {
            
            ResetPath();

        }


        // Update is called once per frame
        void Update()
        {
            if (!path)
                return;

            PathState oldActived = actived;

            actived = mainPathState;
            UpdatePath(mainPathState);
            foreach (var branch in branchs.Values)
            {
                UpdatePath(branch);

                if (branch.distance < branchDistanceThreshold)
                {
                    if (branch.distance < actived.distance)
                    {
                        actived = branch;
                    }
                }
                else
                {
                    if (oldActived != branch)
                        Reset(branch);
                }
            }
            if (actived != oldActived)
            {
                if (oldActived != null)
                {
                    if (!oldActived.isMain)
                    {
                        oldActived.isActive = false;
                    }
                }
                actived.isActive = true;
            }

            if (actived.prevProgressIndex != actived.progressIndex)
            {
                for (int i = actived.prevProgressIndex; i <= actived.progressIndex - 1; i++)
                {
                    int index = i;
                    var point = actived.path.points[index];
                    if (point.events != null)
                    {
                        foreach (var evt in point.events)
                        {
                            TriggerEvent(actived.path, point, evt);
                        }
                    }
                    //Debug.Log("path process index:" + index);
                }
            }

            for (int i = 0; i < actived.path.events.Count; i++)
            {
                var eventInfo = actived.path.events[i];
                //if (actived.processEvents.Contains(i))
                //    continue;
                if (actived.prevProgressDistance < eventInfo.distance && actived.progressDistance >= eventInfo.distance)
                {
                    //actived.processEvents.Add(i);
                    TriggerEvent(actived.path, null, eventInfo);
                }
            }

            if (target)
            {
                target.position = actived.progressPoint.position;
                if (actived.progressPoint.direction.sqrMagnitude > 0.001f)
                    target.rotation = Quaternion.LookRotation(actived.progressPoint.direction);

            }

        }

        void TriggerEvent(WaypointPath path, Waypoint point, WaypointEventInfo eventInfo)
        {
            if (string.IsNullOrEmpty(eventInfo.name))
                return;

            Current = this;
            CurrentEvent = eventInfo;
            WaypointEvent @event = new WaypointEvent()
            {
                Path = path,
                EventInfo = eventInfo,
                Point = point,
                Source = transform
            };
            path.OnEvent(@event);
            //if (eventInfo.valueType != TypeCode.Empty)
            //{
            //    object value = null;
            //    switch (eventInfo.valueType)
            //    {
            //        case TypeCode.Single:
            //            value = eventInfo.floatValue;
            //            break;
            //        case TypeCode.Int32:
            //            value = eventInfo.intValue;
            //            break;
            //        case TypeCode.String:
            //            value = eventInfo.stringValue;
            //            break;

            //    }
            //    path.SendMessage(eventInfo.Function, value, SendMessageOptions.RequireReceiver);
            //}
            //else
            //{
            //    path.SendMessage(eventInfo.Function, SendMessageOptions.RequireReceiver);
            //}
        }


        void UpdatePath(PathState state)
        {
            if (state == null)
                return;
            var path = state.path;

            float dist;
            if (state.path.GetPointToPathDistance(transform.position, out dist))
            {
                state.distance = dist;
            }
            else
            {
                state.distance = float.MaxValue;
            }
            if (state.isMain || state.distance < branchDistanceThreshold)
            {
                state.prevProgressDistance = state.progressDistance;
                state.prevProgressIndex = state.progressIndex;
                if (directionMode)
                {
                    UpdateWithDirection(state);
                }
                else
                {
                    UpdateWithPointToLineDistance(state);
                }

                state.progressPoint = path.GetRoutePoint(state.progressDistance);
            }
        }


        #region 计算进度实现方式: 点到线距离


        void UpdateWithPointToLineDistance(PathState state)
        {
            var path = state.path;

            if (path.closed || state.progressIndex < path.PointCount)
            {
                Vector3 lineStart, lineEnd, lineDelta;

                lineStart = path.GetWorldPosition((state.progressIndex - 1 + path.PointCount) % path.PointCount);
                lineEnd = path.GetWorldPosition(state.progressIndex);
                lineDelta = lineEnd - lineStart;
                Vector3 point;
                WaypointPath.GetSegmentCrossPoint(transform.position, lineStart, lineEnd, out point);

                if (Vector3.Dot(lineDelta, point - lineStart) >= 0f)
                {
                    float dist = (point - lineStart).magnitude;
                    if (dist >= lineDelta.magnitude)
                    {
                        state.progressDistance = path.IndexToDistance(state.progressIndex);
                        int newIndex = state.progressIndex;
                        if (path.closed)
                            newIndex = (state.progressIndex + 1) % path.PointCount;
                        else if (state.progressIndex < path.PointCount - 1)
                            newIndex = state.progressIndex + 1;
                        if (newIndex != state.progressIndex)
                        {
                            state.progressIndex = newIndex;
                            OnWaypointIndexChanged();
                        }
                    }
                    else
                    {
                        float tmp = path.IndexToDistance((state.progressIndex - 1 + path.PointCount) % path.PointCount) + dist;
                        if (tmp > state.progressDistance)
                            state.progressDistance = tmp;
                    }

                }


            }
        }



        #endregion

        #region 计算进度实现方式: 方向

        void UpdateWithDirection(PathState state)
        {
            var path = state.path;

            Vector3 delta = state.progressPoint.position - transform.position;
            if (Vector3.Dot(delta, state.progressPoint.direction) < 0f)
            {
                state.progressDistance += delta.magnitude * 0.5f;
                int newIndex = path.DistanceToIndex(state.progressDistance);
                if (state.progressIndex != newIndex)
                {
                    state.progressIndex = newIndex;
                    OnWaypointIndexChanged();
                }
            }

        }

        #endregion

        void OnWaypointIndexChanged()
        {

        }

        public void SetPath(WaypointPath path)
        {
            this.path = path;
            ResetPath();
        }

        public void Reset(PathState state)
        {
            state.progressIndex = 1;
            state.progressPoint = state.path.GetRoutePoint(0);
            state.progressDistance = 0;
            state.distance = float.MaxValue;
            state.isActive = false;
            state.processEvents.Clear();

        }

        public void ResetPath()
        {
            if (!path)
                return;
            mainPathState = new PathState();
            mainPathState.path = path;
            mainPathState.isMain = true;
            Reset(mainPathState);


            if (target)
            {
                target.position = mainPathState.progressPoint.position;
                target.rotation = Quaternion.LookRotation(mainPathState.progressPoint.direction);
            }

            branchs.Clear();

            foreach (var joint in path.branchs)
            {
                if ((joint.from.path == path && joint.from.HasWaypoint) &&
                    (joint.to.HasWaypoint))
                {
                    if (!branchs.ContainsKey(joint.to.path))
                    {
                        var state = new PathState();
                        state.path = joint.to.path;
                        state.isMain = path == state.path;
                        Reset(state);
                        branchs[joint.to.path] = state;
                    }
                }
            }

            actived = mainPathState;
            var point = path.points[0];
            if (point.events != null)
            {
                foreach (var evt in point.events)
                {
                    TriggerEvent(path, point, evt);
                }
            }

        }

        private void OnDestroy()
        {
            List.Remove(this);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && path)
            {
                DrawState(mainPathState);

                if (actived != mainPathState)
                {
                    DrawState(actived);
                }
            }
        }

        void DrawState(PathState state)
        {
            var path = state.path;
            if (state != actived)
            {
                Gizmos.color = new Color(0, 1, 0, 0.5f);
            }
            else
            {
                Gizmos.color = new Color(0, 1, 0, 1f);
            }
            var routePoint = path.GetRoutePoint(state.progressDistance);
            Gizmos.DrawLine(transform.position, routePoint.position);
            Gizmos.DrawWireSphere(routePoint.position, 1);
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(path.GetWorldPosition(state.progressIndex), 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(routePoint.position, routePoint.direction);
        }
    }

}