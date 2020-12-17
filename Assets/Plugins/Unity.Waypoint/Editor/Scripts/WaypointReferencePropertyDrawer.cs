using UnityEngine;
using UnityEngine.Waypoints;

namespace UnityEditor.Waypoints
{
      
        [CustomPropertyDrawer(typeof(WaypointReference))]
        class WaypointReferencePropertyDrawer : PropertyDrawer
        {

            bool FindPoint(SerializedProperty pathProperty, SerializedProperty pointIdProperty, out int index)
            {
                index = -1;
                var path = pathProperty.objectReferenceValue as WaypointPath;
                if (!path)
                    return false;
                var pointId = pointIdProperty.stringValue;
                if (string.IsNullOrEmpty(pointId))
                    return false;
                index = path.FindWaypointIndexById(pointId);
                return index != -1;
            }


            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty pathProperty = property.FindPropertyRelative("path");
                SerializedProperty pointIdProperty = property.FindPropertyRelative("pointId");
                WaypointPath path = pathProperty.objectReferenceValue as WaypointPath;
                string pointId = pointIdProperty.stringValue;
                WaypointPathEditor.GUIWaypoint(property.displayName, ref path, ref pointId);
                pathProperty.objectReferenceValue = path;
                pointIdProperty.stringValue = pointId;

            }


        }

    }