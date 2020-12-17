using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Waypoints {

    public class WaypointMovement : MonoBehaviour
    {
        public WaypointPath path;
        public bool playOnAwake = true;
        public float speed;
        public bool resetPosition = true;
        private bool isPlaying;
        private float dist;
        public bool freezeRotation;
        [System.NonSerialized]
        public float progress;

        public bool doneInactive;

        void Start()
        {
            Run();
        }

        private void OnEnable()
        {
            if (playOnAwake)
            {
                Run();
            }
            else
            {
                enabled = false;
            }
        }


        private void Run()
        {
            isPlaying = true;
            if (resetPosition)
            {
                dist = 0f;
                progress = 0f;
                var routePoint = path.GetRoutePoint(dist);
                transform.position = routePoint.position;
                var angles = transform.eulerAngles;
                if (freezeRotation)
                {
                    angles = Quaternion.LookRotation(routePoint.direction).eulerAngles;
                }
                else
                {
                    angles.y = Quaternion.LookRotation(routePoint.direction).eulerAngles.y;
                }
                transform.eulerAngles = angles;
            }



        }


        private void Update()
        {
            if (!isPlaying)
                return;
            if (!path)
                return;
            float deltaDist = speed * Time.deltaTime;

            var routepoint = path.GetRoutePoint(dist + deltaDist);

            if (Vector3.Distance(transform.position, routepoint.position) * 0.5f <= deltaDist)
            {
                dist = dist + deltaDist;
            }

            Vector3 delta = routepoint.position - transform.position;
            if (delta.sqrMagnitude > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, routepoint.position, deltaDist);

                var angles = transform.eulerAngles;
                if (freezeRotation)
                {
                    angles = Quaternion.LookRotation(delta.normalized, transform.up).eulerAngles;
                }
                else
                {
                    angles.y = Quaternion.LookRotation(delta.normalized, transform.up).eulerAngles.y;
                }
                transform.eulerAngles = angles;
            }
            progress = dist / path.Length;
            progress = Mathf.Clamp01(progress);
            if (progress >= 1f && doneInactive)
            {
                gameObject.SetActive(false);
            }

        }

    }
}
