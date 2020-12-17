using System.Collections;
using System.Collections.Generic;


namespace UnityEngine.Waypoints
{
    [RequireComponent(typeof(WaypointPath))]
    public class WaypointInstantiate : MonoBehaviour
    {

        public int count;
        public GameObject prefab;
        public Vector3 randomMin;
        public Vector3 randomMax;

        // Use this for initialization
        void Start()
        {

            if (count > 0 && prefab)
            {
                WaypointPath path = GetComponent<WaypointPath>();

                float step;
                if (count > 1)
                    step = path.Length / (count - 1);
                else
                    step = path.Length;

                for (int i = 0; i < count; i++)
                {
                    float dist = step * i;
                    GameObject go = GameObject.Instantiate(prefab, transform);
                    go.SetActive(true);
                    var routePoint = path.GetRoutePoint(dist);
                    Vector3 pos = routePoint.position;
                    pos += Vector3.Lerp(randomMin, randomMax, Random.value);
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.LookRotation(routePoint.direction);
                }
            }
        }

    }
}