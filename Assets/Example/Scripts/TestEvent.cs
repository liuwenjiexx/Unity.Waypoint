using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Waypoints;

public class TestEvent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var path= GetComponent<WaypointPath>();
        path.AddListener( OnWaypointEvent);
    }

    void OnWaypointEvent(WaypointEvent evt)
    {
        Debug.Log(evt.Source.name+": "+ evt.EventInfo.name + ", " + evt.EventInfo.Value);
    }
 
}
