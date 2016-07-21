using UnityEngine;
using System.Collections;

namespace Cameras
{
    [RequireComponent(typeof(RTS_CameraManager))]
    public class TargetFollower : MonoBehaviour 
    {
        private RTS_CameraManager cameraRTS;
        private new Camera camera;
        public string TargetsTag;

        private void Start()
        {
            cameraRTS = gameObject.GetComponent<RTS_CameraManager>();
            camera = gameObject.GetComponent<Camera>();
        }

        private void Update()
        {
            if (TargetsTag != string.Empty)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.transform.CompareTag(TargetsTag))
                            cameraRTS.SetTarget(hit.transform);
                        else
                            cameraRTS.ResetTarget();
                    }
                }
            }
        }
    }
}