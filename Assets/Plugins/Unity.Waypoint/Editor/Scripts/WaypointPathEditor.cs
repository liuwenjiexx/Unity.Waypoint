using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Extensions;
using UnityEngine.Waypoints;
using Object = UnityEngine.Object;
using WaypointEventInfo = UnityEngine.Waypoints.WaypointEventInfo;

namespace UnityEditor.Waypoints
{

    [CustomEditor(typeof(WaypointPath))]
    public class WaypointPathEditor : Editor
    {
        private static WaypointPath lastSelectedPath;
        private static int selectedPointIndex = -1;
        private SceneOperatorMode senceOperatorMode;
        private static WaypointPath selectedPath;
        private SerializedProperty smoothProperty;
        private SerializedProperty visualPointCountProperty;
        private SerializedProperty visualRadiusProperty;
        private SerializedProperty closedProperty;
        private SerializedProperty visualFlagsProperty;
        private static string[] waypointTypes;
        private float activePointDistance;
        private Vector3 activePoint;
        private bool isAttach;
        private float selectPointDistance;
        private int selectedEventIndex;
        private MethodInfo[] eventMethods;

        private bool eventPointDrag;

        private static string DefaultEventName;
        private static Type defaultEventType;
        private static bool isInit;

        private static Type DefaultEventType
        {
            get
            {
                Init();
                return defaultEventType;
            }
        }

        static void Init()
        {
            if (isInit)
                return;
            isInit = true;
            Debug.Log("init");
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
               .Referenced(new Assembly[] { typeof(WaypointEventAttribute).Assembly })
               .SelectMany(o => o.GetTypes()))
            {
                if (type.IsDefined(typeof(WaypointEventAttribute), false))
                {
                    defaultEventType = type;
                    break;
                }
            }

            if (defaultEventType != null)
            {
                var attrs = defaultEventType.GetCustomAttributes(typeof(System.ComponentModel.DefaultValueAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    var valueAttr = attrs[0] as System.ComponentModel.DefaultValueAttribute;
                    DefaultEventName = valueAttr.Value as string;
                }
            }
        }


        void SelectEventPoint(int index)
        {
            selectedEventIndex = index;
            selectedPointIndex = -1;
        }

        void SelectWaypoint(int index)
        {
            selectedPointIndex = index;
            selectedEventIndex = -1;
        }



        private enum SceneOperatorMode
        {
            None,
            AddPoint,
            CreatePath
        }

        private WaypointPath Path { get { return (WaypointPath)target; } }

        public static WaypointPath selected;

        public bool IsLocked
        {
            get
            {
                if (!Path.unlocked)
                    return true;
                return false;
            }
        }


        //[MenuItem("Custom/Create MainPath")]
        static void CreatMainPath()
        {

            GameObject go = new GameObject("MainPath");
            var path = go.AddComponent<WaypointPath>();

            path.unlocked = true;
            if (DefaultEventType != null)
                go.AddComponent(DefaultEventType);

            Undo.RegisterCreatedObjectUndo(go, "Create MainPath");
            Selection.activeObject = go;

        }

        private void OnEnable()
        {
            smoothProperty = serializedObject.FindProperty("smooth");
            closedProperty = serializedObject.FindProperty("closed");
            visualPointCountProperty = serializedObject.FindProperty("visualPointCount");
            visualRadiusProperty = serializedObject.FindProperty("visualRadius");
            visualFlagsProperty = serializedObject.FindProperty("visualFlags");

            if (selected != Path)
            {
                selected = Path;
            }
            eventMethods = GetEventMethods();
            selectedEventIndex = -1;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(string.Format("point count: {0}, distance: {1}", Path.PointCount, Path.Length.ToString("0.#")));

            EditorGUILayout.PropertyField(smoothProperty);
            EditorGUILayout.PropertyField(closedProperty);
            string[] options = new string[] {
                WaypointPath.VisualFlags.ControlPoint.ToString(),
                WaypointPath.VisualFlags.EventPoint.ToString(),
                WaypointPath.VisualFlags.EventName.ToString()};
            visualFlagsProperty.intValue = EditorGUILayout.MaskField(visualFlagsProperty.displayName, visualFlagsProperty.intValue, options);

            EditorGUILayout.PropertyField(visualPointCountProperty);
            EditorGUILayout.PropertyField(visualRadiusProperty);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();


            bool unlocked = Path.unlocked;
            if (unlocked)
            {
                if (GUILayout.Button("Lock"))
                {
                    unlocked = false;
                }
            }
            else
            {
                if (GUILayout.Button("Unlock"))
                {
                    unlocked = true;
                }
            }
            Path.unlocked = unlocked;

            if (IsLocked)
                return;



            var paths = Selection.GetFiltered<WaypointPath>(SelectionMode.Unfiltered);

            //if (GUILayout.Button("Add Joint"))
            //{
            //    m_SelectedPath = Path;
            //    m_SenceOperatorMode = SceneOperatorMode.AddJoint;
            //}

            if (GUILayout.Button("Reverse"))
            {
                Undo.RecordObject(Path, "Reverse");
                Path.points.Reverse();
                EditorUtility.SetDirty(Path);
            }
            if (GUILayout.Button("Reset Local Position"))
            {

                Transform trans = Path.transform;
                if (trans.localPosition != Vector3.zero)
                {
                    Vector3 oldPos = trans.position;
                    Undo.RecordObject(trans, "");
                    trans.localPosition = Vector3.zero;
                    EditorUtility.SetDirty(trans);
                    Vector3 offset = trans.InverseTransformPoint(oldPos);
                    Undo.RecordObject(Path, "");

                    foreach (var p in Path.points)
                    {
                        p.position += offset;
                    }

                    EditorUtility.SetDirty(Path);

                }

            }

            //GUI.enabled = selectedPointIndex >= 0;

            //if (GUILayout.Button("Create Branch"))
            //{
            //    GameObject go = new GameObject("Branch");
            //    go.transform.SetParent(Path.transform);
            //    go.transform.localPosition = Vector3.zero;
            //    go.transform.localEulerAngles = Vector3.zero;
            //    go.transform.localScale = Vector3.one;
            //    var newPath = go.AddComponent<WaypointPath>();
            //    newPath.unlocked = true;
            //    var joint = Path.gameObject.AddComponent<WaypointJoint2>();

            //    joint.from = new WaypointReference()
            //    {
            //        path = Path,
            //        pointId = Path.GetWaypointId(selectedPointIndex)
            //    };
            //    joint.to.path = newPath;
            //    Selection.activeGameObject = go;
            //}
            //GUI.enabled = true;

            using (var checker = new EditorGUI.ChangeCheckScope())
            {
                GUIBranchs();
                if (checker.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(Path);
                }
            }
            if (selectedPointIndex >= 0)
            {

                GUILayout.Label("Waypoint");
                var waypoint = Path.points[selectedPointIndex];
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    using (new GUILayout.VerticalScope())
                    {
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            waypoint.position = EditorGUILayout.Vector3Field(new GUIContent("Position"), waypoint.position);

                            GUILayout.Label("Events");
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(15);
                                using (new GUILayout.VerticalScope())
                                {

                                    if (waypoint.events != null)
                                    {
                                        for (int i = 0; i < waypoint.events.Count; i++)
                                        {
                                            using (new GUILayout.HorizontalScope())
                                            {
                                                waypoint.events[i] = DrawEventInfo(waypoint.events[i]);
                                                if (GUILayout.Button("Del"))
                                                {
                                                    waypoint.events.RemoveAt(i);
                                                    i--;
                                                }
                                            }
                                        }
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        if (GUILayout.Button("Add"))
                                        {
                                            if (waypoint.events == null)
                                            {
                                                waypoint.events = new List<WaypointEventInfo>();

                                            }
                                            var eventInfo = NewEventInfo(0, DefaultEventName);
                                            waypoint.events.Add(eventInfo);
                                            EditorUtility.SetDirty(Path);
                                        }
                                    }

                                }
                            }
                            if (changed.changed)
                            {
                                EditorUtility.SetDirty(Path);
                            }
                        }
                    }
                }
            }


            GUILayout.Label("Event");


            if (selectedEventIndex >= 0 && selectedEventIndex < Path.events.Count)
            {
                WaypointEventInfo eventInfo = Path.events[selectedEventIndex];

                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    eventInfo = DrawEventInfo(eventInfo);

                    if (changed.changed)
                    {
                        Undo.RecordObject(Path, "");
                        Path.events[selectedEventIndex] = eventInfo;
                        EditorUtility.SetDirty(Path);
                    }
                }
            }

        }

        void CreateBranch()
        {

            GameObject go = new GameObject("Branch");
            go.transform.SetParent(Path.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localScale = Vector3.one;
            var newPath = go.AddComponent<WaypointPath>();

            Undo.RecordObject(newPath, "");
            newPath.unlocked = true;


            var joint = new WaypointBranch();
            joint.from = new WaypointReference()
            {
                path = Path,
                pointId = Path.GetWaypointId(selectedPointIndex)
            };
            joint.to.path = newPath;
            Undo.RegisterCreatedObjectUndo(go, "Create Branch");
            Undo.RecordObject(Path, "");
            Path.branchs.Add(joint);
            EditorUtility.SetDirty(target);
            selectedPointIndex = -1;
            Selection.activeGameObject = go;


        }

        void GUIBranchs()
        {
            var path = Path;
            EditorGUILayout.BeginFoldoutHeaderGroup(true, new GUIContent("Branchs"), menuAction: (r) =>
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    var selectedPoint = Path.GetWaypointId(selectedPointIndex);
                    if (string.IsNullOrEmpty(selectedPoint))
                    {
                        Debug.LogError("not select current path point");
                        return;
                    }
                    CreateBranch();
                });
                menu.ShowAsContext();
            });
            EditorGUILayout.EndFoldoutHeaderGroup();
            for (int i = 0; i < path.branchs.Count; i++)
            {
                var joint = path.branchs[i];
                EditorGUI.indentLevel++;
                GUIBranch(ref joint, i);
                path.branchs[i] = joint;
                EditorGUI.indentLevel--;
            }

        }
        void GUIBranch(ref WaypointBranch branch, int index)
        {
            var from = branch.from;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(16);
                EditorGUILayout.BeginFoldoutHeaderGroup(true, new GUIContent("Branch " + index), menuAction: (r) =>
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Delete"), false, () =>
                    {
                        Path.branchs.RemoveAt(index);
                        EditorUtility.SetDirty(target);
                    });
                    menu.ShowAsContext();
                });
                EditorGUILayout.EndFoldoutHeaderGroup();

            }

            GUIWaypoint("From", ref from.path, ref from.pointId, showPath: false);
            if (from.path != Path)
            {
                from.path = Path;
                from.pointId = null;
                GUI.changed = true;
            }
            branch.from = from;

            var to = branch.to;
            GUIWaypoint("To", ref to.path, ref to.pointId);
            branch.to = to;
        }



        WaypointEventInfo DrawEventInfo(WaypointEventInfo eventInfo)
        {
            using (new GUILayout.VerticalScope())
            {

                int funcIndex = -1;

                string[] displayNames = new string[] { "<None>" }.Concat(eventMethods.Select(o => o.Name + "(" + (o.GetParameters().Length > 0 ? o.GetParameters()[0].ParameterType.Name : "") + ")")).ToArray();
                for (int i = 0; i < eventMethods.Length; i++)
                {
                    if (eventMethods[i].Name == eventInfo.Function)
                    {
                        funcIndex = i + 1;
                        break;
                    }
                }

                var newIndex = EditorGUILayout.Popup("Function", funcIndex, displayNames);
                if (newIndex != funcIndex && newIndex != -1)
                {
                    funcIndex = newIndex;
                    if (funcIndex == 0)
                    {
                        eventInfo.Function = null;
                        eventInfo.valueType = TypeCode.Empty;
                    }
                    else
                    {
                        var func = eventMethods[funcIndex - 1];
                        eventInfo.Function = func.Name;
                        if (func.GetParameters().Length > 0)
                            eventInfo.valueType = Type.GetTypeCode(func.GetParameters()[0].ParameterType);
                        else
                            eventInfo.valueType = TypeCode.Empty;
                    }
                }

                switch (eventInfo.valueType)
                {
                    case TypeCode.Int32:
                        eventInfo.intValue = EditorGUILayout.IntField("Int", eventInfo.intValue);
                        break;
                    case TypeCode.Single:
                        eventInfo.floatValue = EditorGUILayout.FloatField("Float", eventInfo.floatValue);
                        break;
                    case TypeCode.String:
                        eventInfo.stringValue = EditorGUILayout.TextField("String", eventInfo.stringValue);
                        break;
                }
            }
            return eventInfo;
        }


        WaypointEventInfo NewEventInfo(float dist, string eventName)
        {
            WaypointEventInfo eventInfo = new WaypointEventInfo();
            eventInfo.distance = dist;
            eventInfo.Function = eventName;
            return eventInfo;
        }
        int AddEventPoint(float dist)
        {
            Undo.RecordObject(Path, "");

            var eventInfo = NewEventInfo(dist, DefaultEventName);
            Path.events.Add(eventInfo);

            EditorUtility.SetDirty(Path);
            return Path.events.Count - 1;
        }

        void DeleteEventPoint(int index)
        {
            if (index >= 0 && index < Path.events.Count)
            {
                Undo.RecordObject(Path, "");
                Path.events.RemoveAt(index);
                EditorUtility.SetDirty(Path);
            }
        }

        MethodInfo[] GetEventMethods()
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            List<MethodInfo> list = new List<MethodInfo>();
            Type[] paramTypes = new Type[] { typeof(string), typeof(int), typeof(float) };
            foreach (var cpt in Path.gameObject.GetComponents<Component>())
            {
                Type type = cpt.GetType();
                if (type == typeof(WaypointPath))
                    continue;

                foreach (var mInfo in type.GetMethods(bindingFlags))
                {
                    var args = mInfo.GetParameters();
                    if (!(args.Length >= 0 && args.Length <= 1))
                        continue;

                    if (!mInfo.DeclaringType.IsSubclassOf(typeof(MonoBehaviour)))
                        continue;

                    //if (args[0].ParameterType != typeof(GameObject))
                    //    continue;
                    if (args.Length > 0 && !paramTypes.Contains(args[0].ParameterType))
                        continue;
                    if (!list.Contains(mInfo))
                        list.Add(mInfo);
                }
            }
            return list.ToArray();
        }


        void UpdateHoverPoint()
        {
            var evt = Event.current;

            var mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            int nearIndex = FindNearIndex(mouseRay);
            float height = 0f;
            isAttach = false;

            if (nearIndex >= 0)
            {
                height = Path.GetWorldPosition(nearIndex).y;
            }

            Plane plane = new Plane(Vector3.up, new Vector3(0, height, 0));
            float dist;
            //Last Point
            if (!plane.Raycast(mouseRay, out dist))
                return;

            int startIndex, endIndex;

            activePoint = mouseRay.GetPoint(dist);

            Vector3 point1, crossPoint;
            if (FindNearLinePoint(mouseRay, out point1, out crossPoint, out startIndex, out endIndex))
            {
                if (Vector3.Distance(HandleUtility.WorldToGUIPoint(point1), HandleUtility.WorldToGUIPoint(crossPoint)) < 15f)
                {
                    isAttach = true;
                    activePoint = crossPoint;
                    nearIndex = startIndex;
                }
            }
            if (nearIndex >= 0)
            {
                activePointDistance = Path.IndexToDistance(nearIndex) + (activePoint - Path.GetWorldPosition(nearIndex)).magnitude;
            }
            else
            {
                activePointDistance = 0f;

            }
            nearIndex = Path.DistanceToIndex(activePointDistance);

        }


        private void OnSceneGUI()
        {
            var evt = Event.current;
            var path = Path;


            //OnSceneWaypointPick();


            path.CachePositionsAndDistances();
            var route = path.GetRoutePoint(0f);
            Handles.ArrowHandleCap(0, route.position, Quaternion.LookRotation(route.direction), HandleUtility.GetHandleSize(route.position) * 0.5f, evt.type);

            if (IsLocked)
                return;

            UpdateHoverPoint();

            Vector3 pos;

            pos = Path.GetRoutePoint(selectPointDistance).position;

            Handles.color = new Color(1, 0, 0, 0.5f);
            if (evt.type == EventType.Repaint)
                Handles.SphereHandleCap(0, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * 0.15f, evt.type);

            Handles.color = Color.white;

            //Add New Point
            if (evt.shift && !evt.control)
            {
                senceOperatorMode = SceneOperatorMode.AddPoint;

                int insertIndex = -1;

                pos = activePoint;
                insertIndex = Path.DistanceToIndex(activePointDistance);


                if (insertIndex < 0 || !isAttach)
                {
                    if (path.PointCount < 1)
                    {
                        insertIndex = 0;
                    }
                    else
                    if (path.points.Count < 2)
                    {
                        insertIndex = 1;
                    }
                    else
                    {
                        if (Vector3.Distance(pos, path.GetWorldPosition(path.points[0])) < Vector3.Distance(pos, path.GetWorldPosition(path.points[path.PointCount - 1])))
                        {
                            insertIndex = 0;
                        }
                        else
                        {
                            insertIndex = path.PointCount;
                        }
                    }

                }




                if (evt.button == 0)
                {
                    if (evt.type == EventType.MouseDown)
                    {
                        AddPoint(path, insertIndex, path.transform.InverseTransformPoint(pos));
                        SelectWaypoint(insertIndex);

                        evt.Use();
                        Selection.activeTransform = path.transform;

                    }
                    else if (evt.type == EventType.MouseUp)
                    {
                        evt.Use();
                    }
                }
                if (!isAttach)
                {
                    if (path.PointCount > 0)
                    {
                        if (insertIndex >= path.PointCount)
                        {
                            Handles.DrawLine(pos, path.GetWorldPosition(path.points[path.PointCount - 1]));
                        }
                        else
                        {
                            Handles.DrawLine(pos, path.GetWorldPosition(path.points[insertIndex]));
                        }

                    }

                }
                if (isAttach)
                {
                    Handles.color = new Color(1, 0, 0, 1f);
                }
                else
                {
                    Handles.color = new Color(0, 0, 1, 1f);
                }
                Handles.Button(pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * 0.1f, 0f, Handles.SphereHandleCap);
                //Handles.SphereHandleCap(0, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * 0.1f, evt.type);

            }
            else if (evt.shift && evt.control)
            {



                if (isAttach)
                {
                    pos = Path.GetRoutePoint(activePointDistance).position;





                    if (isAttach)
                    {
                        Handles.color = new Color(1, 1, 0, 0.5f);
                    }
                    else
                    {
                        Handles.color = new Color(0, 0, 1, 0.5f);
                    }

                    if (Handles.Button(pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * 0.15f, HandleUtility.GetHandleSize(pos) * 0.15f, Handles.SphereHandleCap))
                    {
                        selectPointDistance = activePointDistance;
                        SelectEventPoint(AddEventPoint(selectPointDistance));
                    }
                    Handles.color = Color.white;
                    //if (evt.type == EventType.MouseDown)
                    //{

                    //    SelectEventPoint(AddEventPoint(m_SelectPointDistance));
                    //    Debug.Log("MouseDown");
                    //    evt.Use();
                    //}
                }


            }
            Repaint();



            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Delete)
                {
                    if (selectedPointIndex >= 0)
                    {
                        evt.Use();
                        Undo.RecordObject(path, "Delete Point");
                        path.points.RemoveAt(selectedPointIndex);
                        EditorUtility.SetDirty(path);

                        if (selectedPointIndex >= path.PointCount)
                        {
                            if (path.PointCount == 0)
                            {
                                SelectWaypoint(-1);
                            }
                            else
                            {
                                SelectWaypoint(path.PointCount - 1);
                            }
                        }

                    }

                    if (selectedEventIndex >= 0)
                    {
                        evt.Use();
                        DeleteEventPoint(selectedEventIndex);
                        SelectEventPoint(-1);
                    }

                }
                else if (evt.keyCode == KeyCode.F)
                {
                    EditorApplication.delayCall += () =>
                    {
                        float size = 1;
                        var bounds = GetPathBounds();
                        pos = bounds.center;
                        size = bounds.size.magnitude;
                        if (selectedPointIndex >= 0)
                        {
                            pos = path.GetRoutePosition(path.IndexToDistance(selectedPointIndex));
                            size *= 0.5f;
                        }
                        else if (selectedEventIndex >= 0)
                        {
                            pos = path.GetRoutePosition(path.events[selectedEventIndex].distance);
                            size *= 0.5f;
                        }

                        var view = SceneView.lastActiveSceneView;
                        view.pivot = pos;
                        view.size = size;
                    };
                    evt.Use();
                }
            }


            for (int i = 0; i < path.points.Count; i++)
            {
                var point = path.points[i];
                pos = path.GetWorldPosition(point);
                if (selectedPointIndex == i)
                {
                    Handles.color = new Color(1, 1, 0, 1f);
                }
                else
                {
                    Handles.color = new Color(1, 1, 1, 0.5f);
                }

                float handleSize = HandleUtility.GetHandleSize(pos) * 0.15f;
                if ((path.visualFlags & WaypointPath.VisualFlags.ControlPoint) != 0)
                {
                    if (Handles.Button(pos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                    {
                        if (selectedPointIndex != i)
                        {
                            SelectWaypoint(i);
                            Repaint();
                        }
                    }
                }
                string label = "";
                if ((path.visualFlags & WaypointPath.VisualFlags.EventName) != 0)
                {
                    label += WaypointEventsToString(point);
                }

                if (!string.IsNullOrEmpty(label))
                    Handles.Label(pos, new GUIContent(label));

                if (selectedPointIndex == i)
                {
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        pos = Handles.PositionHandle(pos, Quaternion.identity);
                        if (scope.changed)
                        {
                            Undo.RecordObject(path, "Move Point");
                            point.position = path.transform.InverseTransformPoint(pos);
                            EditorUtility.SetDirty(path);
                        }
                    }

                }
                Handles.color = Color.white;
                if (path.visualRadius > 0)
                {
                    var routePoint = path.GetRoutePoint(path.IndexToDistance(i));
                    Handles.CircleHandleCap(0, routePoint.position, Quaternion.LookRotation(routePoint.direction), path.visualRadius, evt.type);
                }

            }


            for (int i = 0; i < path.branchs.Count; i++)
            {
                var joint = path.branchs[i];
                DrawBranch(joint.from, joint.to);
            }


            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var obj = HandleUtility.PickGameObject(evt.mousePosition, false) as GameObject;
                if (obj && obj.GetComponent<WaypointPath>() == target)
                {

                    evt.Use();
                }
                else
                {
                    SelectWaypoint(-1);
                }
            }

            if (evt.type == EventType.KeyDown)
            {
                if (selectedEventIndex >= 0 && evt.keyCode == KeyCode.Escape)
                {
                    SelectEventPoint(-1);
                }
            }

            if ((path.visualFlags & WaypointPath.VisualFlags.EventPoint) != 0)
            {
                eventPointDrag = evt.alt;

                for (int i = 0; i < path.events.Count; i++)
                {
                    var eventInfo = path.events[i];
                    var point = path.GetRoutePoint(eventInfo.distance);
                    if (selectedEventIndex == i)
                    {

                        Vector2 pos1 = HandleUtility.WorldToGUIPoint(point.position);
                        float size = 30;

                        var rect = new Rect(pos1.x - size * 0.5f, pos1.y - size * 0.5f, size, size);

                        if (!eventPointDrag && evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
                        {
                            eventPointDrag = true;
                            //Undo.RecordObject(path, "");
                            evt.Use();
                        }

                        if (eventPointDrag)
                        {
                            if (evt.type == EventType.MouseMove)
                            {
                                if (isAttach)
                                {
                                    eventInfo.distance = activePointDistance;
                                    path.events[i] = eventInfo;
                                }
                                evt.Use();
                            }
                            else if (evt.type == EventType.MouseUp)
                            {
                                //Undo.RecordObject(path, "");
                                //path.events[i] = eventInfo;
                                //EditorUtility.SetDirty(path);
                                eventPointDrag = false;
                                evt.Use();
                            }
                        }
                        /*   pos = Handles.PositionHandle(point.position, Quaternion.identity);

                           Vector3 crossPoint;
                           float dist = PointToDistance(pos, out crossPoint);
                           if (dist != eventInfo.distance)
                           {
                               eventInfo.distance = dist;
                               path.events[i] = eventInfo;
                           }*/
                        Handles.color = new Color(1, 1, 0, 1);

                    }
                    else
                    {
                        Handles.color = new Color(1, 1, 0, 0.3f);
                    }


                    if (Handles.Button(point.position, Quaternion.LookRotation(Vector3.up), HandleUtility.GetHandleSize(point.position) * 0.15f, HandleUtility.GetHandleSize(point.position) * 0.1f, Handles.SphereHandleCap))
                    {
                        SelectEventPoint(i);
                        eventPointDrag = false;
                    }

                    if ((path.visualFlags & WaypointPath.VisualFlags.EventName) != 0)
                    {
                        if (!string.IsNullOrEmpty(eventInfo.Function))
                        {
                            Handles.Label(point.position, eventInfo.Function);
                        }
                        else
                        {
                            Handles.Label(point.position, "<Empty>");
                        }
                    }
                }

                Handles.color = Color.white;
            }
            OnSceneWaypointPick();

        }


        void DrawBranch(WaypointReference from, WaypointReference to)
        {
            float radius = 0.3f;
            bool hasFrom = false, hasTo = false;

            Waypoint fromPoint, toPoint;
            Vector3 fromPos = Vector3.zero, toPos = Vector3.zero;

            if (from.GetWaypoint(out fromPoint))
            {
                Handles.color = new Color(1, 0, 0, 1);
                fromPos = from.path.GetWorldPosition(fromPoint.position);
                Handles.SphereHandleCap(0, fromPos, Quaternion.identity, HandleUtility.GetHandleSize(fromPos) * radius, Event.current.type);
                hasFrom = true;
                Handles.color = Color.white;
            }

            if (to.GetWaypoint(out toPoint))
            {
                Handles.color = new Color(0, 0, 1, 1);
                toPos = to.path.GetWorldPosition(toPoint.position);
                Handles.SphereHandleCap(0, toPos, Quaternion.identity, HandleUtility.GetHandleSize(toPos) * radius, Event.current.type);
                hasTo = true;
                Handles.color = Color.white;
            }

            if (hasFrom && hasTo)
            {
                Handles.DrawLine(fromPos, toPos);
            }
        }

        private WaypointPath activePath;
        private int activePointIndex;
        public static bool isPick;
        public static PickCallbackDelegate PickCallback;

        public delegate void PickCallbackDelegate(WaypointPath path, int index, bool success);


        void OnSceneWaypointPick()
        {
            if (!isPick)
                return;

            var evt = Event.current;

            var path = selectedPath;
            var pointIndex = selectedPointIndex;
            if (activePath)
            {
                path = activePath;
                pointIndex = activePointIndex;
            }

            if (evt.type == EventType.MouseMove)
            {
                // if (allowChangedPath)
                {

                    var go = HandleUtility.PickGameObject(evt.mousePosition, false);
                    if (go)
                    {
                        var path2 = go.GetComponent<WaypointPath>();
                        if (path2 && path2 != activePath)
                        {
                            if (path2 == selectedPath)
                            {
                                activePath = null;
                            }
                            else
                            {
                                activePath = path2;
                                activePointIndex = -1;
                            }
                        }

                    }

                }
            }


            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    isPick = false;
                    PickCallback(selectedPath, selectedPointIndex, false);
                    PickCallback = null;
                    evt.Use();
                }
            }


            if (path)
            {

                for (int i = 0; i < path.points.Count; i++)
                {
                    var point = path.points[i];
                    Vector3 pos = path.GetWorldPosition(point);
                    Handles.color = Color.white;
                    Handles.Label(pos, point.ToString());
                    if (i == pointIndex)
                    {
                        Handles.color = new Color(0, 0, 1, 0.5f);
                    }
                    else
                    {
                        Handles.color = new Color(1, 1, 0, 0.5f);
                    }

                    float handleSize = HandleUtility.GetHandleSize(pos) * 0.15f;

                    if (Handles.Button(pos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                    {
                        selectedPath = path;
                        selectedPointIndex = i;

                        if (PickCallback != null)
                        {
                            PickCallback(selectedPath, selectedPointIndex, true);
                            PickCallback = null;
                        }
                        Repaint();
                    }

                }
                Handles.color = Color.white;

            }

        }


        string WaypointEventsToString(Waypoint waypoint)
        {

            StringBuilder sb = new StringBuilder();
            if (waypoint.events != null)
            {
                for (int i = 0; i < waypoint.events.Count; i++)
                {
                    var eventInfo = waypoint.events[i];
                    if (!string.IsNullOrEmpty(eventInfo.Function))
                    {
                        if (sb.Length > 0)
                            sb.Append(",");
                        sb.Append(eventInfo.Function);
                    }
                }
            }
            return sb.ToString();
        }


        static void FindNear2Point(Vector3[] a, Vector3[] b, out int aIndex, out int bIndex)
        {
            float n;

            n = Mathf.Infinity;
            aIndex = -1;
            bIndex = -1;
            float n2;
            for (int i = 0; i < a.Length; i++)
            {
                Vector3 aPoint = a[i];

                for (int j = 0; j < b.Length; j++)
                {
                    n2 = Vector3.SqrMagnitude(aPoint - b[j]);
                    if (n2 < n)
                    {
                        aIndex = i;
                        bIndex = j;
                        n = n2;
                    }
                }
            }

        }

        public float GetDistance(int startIndex, int endIndex)
        {
            var path = Path;
            return Vector3.Distance(path.GetWorldPosition(path.points[startIndex]), path.GetWorldPosition(path.points[endIndex]));
        }

        public int FindNearIndex(Ray ray)
        {
            Vector3 position;
            return FindNearIndex(ray, out position);
        }
        public int FindNearIndex(Ray ray, out Vector3 position)
        {
            position = Vector3.zero;
            if (Path.PointCount == 0)
                return -1;
            float dist = float.MaxValue;

            int index = -1;
            for (int i = 0; i < Path.PointCount; i++)
            {
                var point = Path.points[i];
                Vector3 pos = Path.GetWorldPosition(point);
                Plane plane = new Plane(Vector3.up, new Vector3(0f, pos.y, 0f));
                float d;
                if (plane.Raycast(ray, out d))
                {
                    Vector3 rayPoint = ray.GetPoint(d);
                    d = Vector3.Distance(pos, rayPoint);
                    if (d < dist)
                    {
                        position = rayPoint;
                        dist = d;
                        index = i;
                    }

                }
            }
            return index;
        }




        public bool FindNearLinePoint(Ray ray, out Vector3 point, out Vector3 crossPoint, out int startIndex, out int endIndex)
        {
            point = Vector3.zero;
            crossPoint = Vector3.zero;
            startIndex = -1;
            endIndex = -1;
            if (Path.PointCount < 2)
            {
                return false;
            }
            Vector3 pos;

            float dist = float.MaxValue;

            float stepDist = (Path.Length / Path.visualPointCount);
            for (float dist2 = 0f; dist2 < Path.Length; dist2 += stepDist)
            {
                int index = Path.DistanceToIndex(dist2);
                var routePoint = Path.GetRoutePoint(dist2);
                Plane plane = new Plane(Vector3.up, new Vector3(0f, routePoint.position.y, 0f));
                float d;
                if (!plane.Raycast(ray, out d))
                    continue;
                pos = ray.GetPoint(d);
                Vector3 tmpCrossPoint;
                if (WaypointPath.GetSegmentCrossPoint(pos, routePoint.position, routePoint.position + routePoint.direction * stepDist, out tmpCrossPoint))
                {
                    if (Vector3.Distance(pos, tmpCrossPoint) < dist)
                    {
                        point = pos;
                        startIndex = (index - 1 + Path.PointCount) % Path.PointCount;
                        endIndex = index;
                        crossPoint = tmpCrossPoint;
                        dist = Vector3.Distance(pos, tmpCrossPoint);
                    }
                }
            }

            if (dist == float.MaxValue)
                return false;

            return true;
        }

        public float PointToDistance(Vector3 point, out Vector3 crossPoint)
        {
            crossPoint = new Vector3();
            int startIndex = -1, endIndex = -1;
            float dist = float.MaxValue;
            float stepDist = (Path.Length / Path.visualPointCount);
            for (float dist2 = 0f; dist2 < Path.Length; dist2 += stepDist)
            {
                int index = Path.DistanceToIndex(dist2);
                var routePoint = Path.GetRoutePoint(dist2);
                Vector3 tmpCrossPoint;
                if (WaypointPath.GetSegmentCrossPoint(point, routePoint.position, routePoint.position + routePoint.direction * stepDist, out tmpCrossPoint))
                {
                    if (Vector3.Distance(point, tmpCrossPoint) < dist)
                    {
                        startIndex = (index - 1 + Path.PointCount) % Path.PointCount;
                        endIndex = index;
                        crossPoint = tmpCrossPoint;
                        dist = Vector3.Distance(point, tmpCrossPoint);
                    }
                }
            }
            if (startIndex >= 0)
            {
                return Path.IndexToDistance(startIndex) + Vector3.Distance(Path.GetWorldPosition(startIndex), crossPoint);
            }

            return 0f;
        }


        Bounds GetPathBounds()
        {
            if (Path.PointCount == 0)
                return new Bounds(Path.transform.position, Vector3.one);
            Bounds bounds;
            var points = Path.points.Select(o => Path.GetWorldPosition(o.position));
            GetBounds(points, out bounds);
            return bounds;
        }
        public static bool GetBounds(IEnumerable<Vector3> points, out Bounds bounds)
        {
            float xMin = 0, xMax = 0, yMin = 0, yMax = 0, zMin = 0, zMax = 0;
            bool first = true;
            foreach (Vector3 pt in points)
            {
                if (first)
                {
                    xMin = xMax = pt.x;
                    yMin = yMax = pt.y;
                    zMin = zMax = pt.z;
                    first = false;
                }
                else
                {
                    if (pt.x < xMin)
                        xMin = pt.x;
                    else if (pt.x > xMax)
                        xMax = pt.x;

                    if (pt.y < yMin)
                        yMin = pt.y;
                    else if (pt.y > yMax)
                        yMax = pt.y;

                    if (pt.z < zMin)
                        zMin = pt.z;
                    else if (pt.z > zMax)
                        zMax = pt.z;
                }

            }
            Vector3 size = new Vector3(xMax - xMin, yMax - yMin, zMax - zMin);

            bounds = new Bounds(new Vector3(xMin, yMin, zMin) + size * 0.5f, size);

            return !first;
        }

        void SetOperatorMode(SceneOperatorMode mode)
        {
            if (senceOperatorMode == mode)
            {
                senceOperatorMode = SceneOperatorMode.None;
            }
            else
            {
                senceOperatorMode = mode;
            }
        }
        public Waypoint AddPoint(WaypointPath path, int index, Vector3 position)
        {
            Undo.RecordObject(path, "");
            Waypoint point = new Waypoint();
            point.position = position;
            if (point.events == null)
                point.events = new List<WaypointEventInfo>();
            var eventInfo = NewEventInfo(0, DefaultEventName);
            //eventInfo.Function = WaypointPathEvent.EventName_ResetWaypointForward;
            point.events.Add(eventInfo);

            path.points.Insert(index, point);


            path.CachePositionsAndDistances();
            EditorUtility.SetDirty(path);
            return point;
        }


        public class WaypointPickState
        {
            public bool isPick;
            public Object target;
            public WaypointReference waypointRef;
            public bool changed;
            public WaypointPath path;
            public int pointIndex;
            public static WaypointPickState current;
            public static void Pick(WaypointPickState newState)
            {

                if (WaypointPickState.current != null)
                    WaypointPickState.current.isPick = false;

                WaypointPickState.current = newState;
                if (newState != null)
                {
                    newState.isPick = true;
                    newState.changed = false;
                    WaypointPathEditor.isPick = true;
                    WaypointPathEditor.PickCallback = (path, index, success) =>
                    {

                        if (WaypointPickState.current != null)
                        {
                            if (success)
                            {
                                //current.waypointRef.path = path;
                                //if (path)
                                //    current.waypointRef.pointId = path.GetWaypointId(index);
                                //else
                                //    current.waypointRef.pointId = null;
                                WaypointPickState.current.changed = true;
                            }
                            else
                            {
                                WaypointPickState.current.isPick = false;
                            }
                        }

                        WaypointPathEditor.PickCallback = null;


                    };
                }

            }
        }



        public static void GUIWaypointReference(string label, WaypointReference waypointReference)
        {
            GUIWaypoint(label, ref waypointReference.path, ref waypointReference.pointId);
        }

        public static void GUIWaypoint(string label, ref WaypointPath path, ref string pointId, bool showPath = true)
        {
            var ctrlId = GUIUtility.GetControlID(typeof(WaypointPickState).GetHashCode(), FocusType.Passive);
            WaypointPickState state = (WaypointPickState)GUIUtility.GetStateObject(typeof(WaypointPickState), ctrlId);

            if (state.isPick)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        state.isPick = false;
                        WaypointPathEditor.isPick = false;
                        PickCallback = null;
                        Event.current.Use();
                    }
                }
                if (state.changed)
                {
                    if (selectedPath)
                    {
                        if (path != selectedPath)
                        {
                            path = selectedPath;
                            GUI.changed = true;
                        }
                        string newPointId = path.GetWaypointId(selectedPointIndex);
                        if (pointId != newPointId)
                        {
                            pointId = newPointId;
                            GUI.changed = true;
                        }
                    }
                    else
                    {
                        if (pointId != null)
                        {
                            pointId = null;
                            GUI.changed = true;
                        }
                    }
                    state.changed = false;
                    state.isPick = false;
                    selectedPath = null;
                    selectedPointIndex = -1;
                    WaypointPathEditor.isPick = false;
                    GUI.changed = true;
                    WaypointPickState.Pick(null);
                }
            }


            EditorGUILayout.LabelField(label);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(15);
                using (new GUILayout.VerticalScope())
                {
                    if (showPath)
                    {
                        path = (WaypointPath)EditorGUILayout.ObjectField(new GUIContent("Path"), path, typeof(WaypointPath), true);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        int pointIndex;
                        Waypoint waypoint = null;

                        if (!FindPoint(path, pointId, out pointIndex))
                        {
                            GUI.color = Color.red;
                        }
                        else
                        {
                            GUI.color = Color.white;
                            waypoint = path.points[pointIndex];
                        }

                        if (state.isPick)
                        {
                            GUI.color = Color.blue;
                        }
                        EditorGUILayout.PrefixLabel("Point");

                        GUILayout.Label(waypoint == null ? "null" : waypoint.ToString(), GUILayout.ExpandWidth(true));

                        if (GUILayout.Button(state.isPick ? "Unpick" : "Pick"))
                        {
                            if (state.isPick)
                            {
                                state.isPick = false;
                                WaypointPickState.Pick(null);

                            }
                            else
                            {
                                state.isPick = true;
                                state.changed = false;
                                state.path = path;
                                state.pointIndex = pointIndex;
                                selectedPath = path;
                                selectedPointIndex = pointIndex;
                                WaypointPickState.Pick(state);
                            }
                        }
                        GUI.enabled = !string.IsNullOrEmpty(pointId);
                        if (GUILayout.Button("Clear"))
                        {
                            pointId = null;
                            state.isPick = false;
                            PickCallback = null;
                            GUI.changed = true;
                        }

                        GUI.color = Color.white;
                    }
                }
            }

        }

        static bool FindPoint(WaypointPath path, string pointId, out int index)
        {
            index = -1;
            if (!path)
                return false;
            if (string.IsNullOrEmpty(pointId))
                return false;
            index = path.FindWaypointIndexById(pointId);
            return index != -1;
        }
    }

}