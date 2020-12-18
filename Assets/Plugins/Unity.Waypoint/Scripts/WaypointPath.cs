using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

namespace UnityEngine.Waypoints
{

    public class WaypointPath : MonoBehaviour
    {
        [SerializeField]
        public List<Waypoint> points = new List<Waypoint>();

        public List<WaypointEventInfo> events = new List<WaypointEventInfo>();

        public List<WaypointBranch> branchs = new List<WaypointBranch>();


        private Vector3[] cachedPoints;
        private float[] cachedDistances;
        public bool smooth = true;
        public bool closed;
        public bool unlocked;

        [NonSerialized]
        public List<Action<WaypointEvent>> listeners = new List<Action<WaypointEvent>>();

        private static WaypointPath main;

        #region Editor
        public VisualFlags visualFlags = VisualFlags.All;
        public int visualPointCount = 100;
        public float visualRadius = 0;
        #endregion


        public int PointCount
        {
            get { return points.Count; }
        }

        public float Length
        {
            get; private set;
        }

        private float[] CachedDistances
        {
            get
            {
                if (cachedDistances == null)
                    CachePositionsAndDistances();
                return cachedDistances;
            }
        }
        private Vector3[] CachedPoints
        {
            get
            {
                if (cachedPoints == null)
                    CachePositionsAndDistances();
                return cachedPoints;
            }
        }

        public static WaypointPath Main
        {
            get
            {
                if (!main)
                    Debug.LogError("MainPath null");
                return main;
            }
        }

        private void Awake()
        {
            CachePositionsAndDistances();
            events.Sort(EventInfoComparer.Instance);

            if (name == "MainPath")
            {
                main = this;
            }
            else if (!main)
            {
                main = this;
            }
        }


        public Vector3 GetWorldPosition(Waypoint point)
        {
            return transform.TransformPoint(point.position);
        }
        public Vector3 GetWorldPosition(int index)
        {
            return GetWorldPosition(points[index % PointCount]);
        }

        public Vector3 GetWorldPosition(Vector3 point)
        {
            return transform.TransformPoint(point);
        }

        public void CachePositionsAndDistances()
        {
            int length = PointCount;
            if (length == 0)
            {
                cachedPoints = new Vector3[0];
                cachedDistances = new float[0];
                Length = 0;
                return;
            }

            if (closed)
                length++;

            cachedPoints = new Vector3[length];
            cachedDistances = new float[length];

            float totalDist = 0f;
            Vector3 p1, p2;
            for (int i = 0; i < length; i++)
            {
                p1 = GetWorldPosition(i);
                if (closed)
                {
                    p2 = GetWorldPosition((i + 1) % PointCount);
                }
                else
                {
                    p2 = GetWorldPosition(i < PointCount - 1 ? i + 1 : i);
                }

                cachedPoints[i] = p1;
                cachedDistances[i] = totalDist;
                totalDist += Vector3.Distance(p1, p2);

            }

            Length = cachedDistances[length - 1];
        }



        public Vector3 GetRoutePosition(float dist)
        {
            if (CachedDistances.Length == 0)
                return transform.position;

            if (CachedPoints.Length == 1)
                return CachedPoints[0];

            float totalDist = cachedDistances[cachedDistances.Length - 1];

            if (closed)
                dist = dist % totalDist;

            int index = DistanceToIndex(dist);

            int p1, p2;

            if (closed)
                p1 = (index - 1 + PointCount) % PointCount;
            else
                p1 = index > 0 ? index - 1 : 0;

            p2 = index;
            float t = Mathf.InverseLerp(cachedDistances[p1], cachedDistances[p2], dist);

            if (smooth)
            {
                int p0, p3;

                if (closed)
                    p0 = (p1 - 1 + PointCount) % PointCount;
                else
                {
                    if (p2 < 2)
                    {
                        return Vector3.Lerp(cachedPoints[p1], cachedPoints[p2], t);
                    }
                    p0 = p1 > 0 ? p1 - 1 : p1;
                }
                if (closed)
                    p3 = (p2 + 1) % PointCount;
                else
                {
                    if (p2 > PointCount - 2)
                    {
                        return Vector3.Lerp(cachedPoints[p1], cachedPoints[p2], t);
                    }
                    p3 = p2 < PointCount - 1 ? p2 + 1 : p2;
                }
                return CatmullRom(cachedPoints[p0], cachedPoints[p1], cachedPoints[p2], cachedPoints[p3], t);
            }
            else
            {
                return Vector3.Lerp(cachedPoints[p1], cachedPoints[p2], t);
            }


        }


        public RoutePoint GetRoutePoint(float dist)
        {
            Vector3 p1 = GetRoutePosition(dist);
            Vector3 p2 = GetRoutePosition(dist + 0.1f);
            Vector3 dir = (p2 - p1).normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;
            return new RoutePoint(p1, dir);
        }



        public int DistanceToIndex(float dist)
        {
            if (PointCount <= 0)
                return -1;
            int index = 0;

            if (closed)
            {
                dist = dist % CachedDistances[cachedDistances.Length - 1];
            }

            while (CachedDistances[index] < dist)
            {
                if (index >= cachedDistances.Length - 1)
                    break;

                ++index;
            }

            return index;
        }

        public float IndexToDistance(int index)
        {
            if (index < 0)
                return 0f;
            return CachedDistances[index];
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            return 0.5f * ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
        }


        void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        void DrawGizmos(bool selected)
        {
            CachePositionsAndDistances();
            if (selected)
            {
                Gizmos.color = new Color(1, 1, 0, 1f);
            }
            else
            {
                Gizmos.color = new Color(1, 1, 0, 0.5f);
            }

            if (points.Count > 1)
            {

                Vector3 prev = new Vector3();
                Vector3 next;
                bool first = true;
                if (smooth)
                {
                    float totalDist = cachedDistances[cachedDistances.Length - 1];
                    float stepDist = totalDist / visualPointCount;
                    for (float dist = 0f; dist <= totalDist; dist += stepDist)
                    {
                        next = GetRoutePosition(dist);
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            Gizmos.DrawLine(prev, next);
                        }

                        prev = next;
                    }

                }
                else
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        var p = points[i];
                        next = GetWorldPosition(p.position);
                        if (first)
                        {
                            first = false;
                            if (closed)
                            {
                                prev = GetWorldPosition(PointCount - 1);
                                Gizmos.DrawLine(prev, next);
                            }
                        }
                        else
                        {
                            Gizmos.DrawLine(prev, next);
                        }
                        prev = next;
                    }
                }
            }


        }


        public Waypoint FindWaypointById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (var p in points)
            {
                if (p.id == id)
                    return p;
            }
            return null;
        }

        public int FindWaypointIndexById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.id == id)
                    return i;
            }
            return -1;
        }

        public string GetWaypointId(int index)
        {
            if (index < 0 || index >= PointCount)
                return null;
            var point = points[index];
            if (string.IsNullOrEmpty(point.id))
            {
                point.id = System.Guid.NewGuid().ToString("N");
                //#if UNITY_EDITOR
                //                EditorUtility.SetDirty(this);
                //#endif
            }
            return point.id;
        }

        public bool GetPointToPathDistance(Vector3 point, out float dist)
        {
            return GetPointToPathDistance(point, cachedPoints, out dist);
        }

        public static bool GetPointToPathDistance(Vector3 point, IEnumerable<Vector3> points, out float dist)
        {
            bool ret = false;
            dist = float.MaxValue;
            bool first = true;
            Vector3 prev = new Vector3();
            Vector3 crossPoint;
            foreach (var p in points)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (GetSegmentCrossPoint(point, prev, p, out crossPoint))
                    {
                        dist = Mathf.Min(dist, Vector3.Distance(point, crossPoint));
                        ret = true;
                    }
                }
                prev = p;
            }
            return ret;
        }

        public static bool GetSegmentCrossPoint(Vector3 point, Vector3 lineStart, Vector3 lineEnd, out Vector3 crossPoint)
        {
            Vector3 v = lineEnd - lineStart;
            Vector3 w = point - lineStart;

            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0)
            {
                crossPoint = lineStart;
                return false;
            }
            float c2 = Vector3.Dot(v, v);

            if (c2 <= c1)
            {
                crossPoint = lineEnd;
                return false;
            }
            crossPoint = lineStart + c1 / c2 * v;
            return true;
        }


        public void AddListener(Action<WaypointEvent> l)
        {
            listeners.Add(l);
        }

        public void RemoveListener(Action<WaypointEvent> l)
        {
            listeners.Remove(l);
        }

        public void OnEvent(WaypointEvent @event)
        {

            foreach (var l in listeners)
            {
                l(@event);
            }
        }


        class EventInfoComparer : IComparer<WaypointEventInfo>
        {
            public static readonly EventInfoComparer Instance = new EventInfoComparer();
            public int Compare(WaypointEventInfo x, WaypointEventInfo y)
            {
                if (x.distance == y.distance)
                    return 0;
                return x.distance < y.distance ? -1 : 1;
            }
        }

        [System.Flags]
        public enum VisualFlags
        {
            None = 0,
            ControlPoint = 0x1,
            EventPoint = 0x2,
            EventName = 0x4,
            All = 0xFFFF,
        }

    }


    public class WaypointEventAttribute : Attribute
    {

    }

    public class WaypointEvent
    {
        public WaypointPath Path { get; set; }
        public Waypoint Point { get; set; }
        public WaypointEventInfo EventInfo { get; set; }
        public Transform Source { get; set; }
    }

}