using UnityEngine;
using System.Collections;

/*
 Original scripts from asset store item RTS camera:
 	D. Sylkin. RTS camera, Version 1.0,  [Online]. Available: https://www.assetstore.unity3d.com/en/#!/content/43321.
 Modified by:
 	Francis Charette Migneault
*/
namespace Cameras
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("RTS Camera")]
	public class RTS_CameraManager : MonoBehaviour
    {

        #region Foldouts

#if UNITY_EDITOR

        public int lastTab = 0;

        public bool movementSettingsFoldout;
        public bool zoomingSettingsFoldout;
        public bool rotationSettingsFoldout;
        public bool heightSettingsFoldout;
        public bool mapLimitSettingsFoldout;
        public bool targetingSettingsFoldout;
        public bool inputSettingsFoldout;

#endif

        #endregion
		        
        public bool useFixedUpdate = false; 				//use FixedUpdate() or Update()

        #region Movement

        public float keyboardMovementSpeed = 5f; 			//speed with keyboard movement
        public float screenEdgeMovementSpeed = 3f; 			//spee with screen edge movement
        public float followingSpeed = 5f; 					//speed when following a target
        public float rotationSpeed = 3f;
        public float panningSpeed = 10f;
        public float mouseRotationSpeed = 10f;

        #endregion

        #region Height

        public bool autoHeight = true;
        public LayerMask groundMask = -1; 					//layermask of ground or other objects that affect height

        public float maxHeight = 10f;						//maximal height
        public float minHeight = 15f; 						//minimnal height
        public float heightDampening = 5f; 
        public float keyboardZoomingSensitivity = 2f;
        public float scrollWheelZoomingSensitivity = 25f;

        private float zoomPos = 0; 							//value in range (0, 1) used as t in Matf.Lerp
		private GameObject scrolling;

        #endregion

        #region MapLimits

        public bool limitMap = true;
        public float limitX = 50f; 							//x limit of map
        public float limitY = 50f; 							//z limit of map

        #endregion

        #region Targeting

        public Transform targetFollow; 						//target to follow
        public Vector3 targetOffset;

        /// <summary>
        /// are we following target
        /// </summary>
        public bool FollowingTarget
        {
            get { return targetFollow != null; }
        }

        #endregion

        #region Input

        public bool useScreenEdgeInput = true;
        public float screenEdgeBorder = 25f;

        public bool useKeyboardInput = true;
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        public bool usePanning = true;
        public KeyCode panningKey = KeyCode.Mouse2;

        public bool useKeyboardZooming = true;
        public KeyCode zoomInKey = KeyCode.E;
        public KeyCode zoomOutKey = KeyCode.Q;

        public bool useScrollwheelZooming = true;
		public bool invertScrollDirection = false;
        public string zoomingAxis = "Mouse ScrollWheel";

        public bool useKeyboardRotation = true;
        public KeyCode rotateRightKey = KeyCode.X;
        public KeyCode rotateLeftKey = KeyCode.Z;

        public bool useMouseRotation = true;
        public KeyCode mouseRotationKey = KeyCode.Mouse1;

        public bool useMouseRotationSecondaryKey = true;
        public KeyCode mouseRotationSecondaryKey = KeyCode.LeftControl;
        public KeyCode mouseRotationSecondaryKeyOther = KeyCode.RightControl;

        private Vector2 KeyboardInput
        {
            get { return useKeyboardInput ? new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis)) : Vector2.zero; }
        }

        private Vector2 MouseInput
        {
            get { return Input.mousePosition; }
        }

        private float ScrollWheel
        {
            get { return Input.GetAxis(zoomingAxis); }
        }

        private Vector2 MouseAxis
        {
            get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        }

        private int ZoomDirection
        {
            get
            {
                bool zoomIn = Input.GetKey(zoomInKey);
                bool zoomOut = Input.GetKey(zoomOutKey);
                if (zoomIn && zoomOut)
                    return 0;
                else if (!zoomIn && zoomOut)
                    return 1;
                else if (zoomIn && !zoomOut)
                    return -1;
                else 
                    return 0;
            }
        }

        private int RotationDirection
        {			
            get
            {
                bool rotateRight = Input.GetKey(rotateRightKey);
                bool rotateLeft = Input.GetKey(rotateLeftKey);
                if(rotateLeft && rotateRight)
                    return 0;
                else if(rotateLeft && !rotateRight)
                    return -1;
                else if(!rotateLeft && rotateRight)
                    return 1;
                else 
                    return 0;
            }
        }

        #endregion

        #region Unity_Methods

        private void Start()
        {                        
			scrolling = new GameObject();
        }

        private void Update()
        {
            if (!useFixedUpdate)
                CameraUpdate();
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
                CameraUpdate();
        }

        #endregion

        #region RTSCamera_Methods

        /// <summary>
        /// update camera movement and rotation
        /// </summary>
        private void CameraUpdate()
        {
            if (FollowingTarget)
                FollowTarget();
            else
                Move();

            Rotation();
			Zoom();
            LimitPosition();
        }

        /// <summary>
        /// move camera with keyboard or with screen edge
        /// </summary>
        private void Move()
        {
            if (useKeyboardInput)
            {
                Vector3 desiredMove = new Vector3(KeyboardInput.x, 0, KeyboardInput.y);

                desiredMove *= keyboardMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = transform.InverseTransformDirection(desiredMove);

                transform.Translate(desiredMove, Space.Self);
            }

            if (useScreenEdgeInput)
            {
                Vector3 desiredMove = new Vector3();

                Rect leftRect = new Rect(0, 0, screenEdgeBorder, Screen.height);
                Rect rightRect = new Rect(Screen.width - screenEdgeBorder, 0, screenEdgeBorder, Screen.height);
                Rect upRect = new Rect(0, Screen.height - screenEdgeBorder, Screen.width, screenEdgeBorder);
                Rect downRect = new Rect(0, 0, Screen.width, screenEdgeBorder);

                desiredMove.x = leftRect.Contains(MouseInput) ? -1 : rightRect.Contains(MouseInput) ? 1 : 0;
                desiredMove.z = upRect.Contains(MouseInput) ? 1 : downRect.Contains(MouseInput) ? -1 : 0;

                desiredMove *= screenEdgeMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = transform.InverseTransformDirection(desiredMove);

                transform.Translate(desiredMove, Space.Self);
            }       
        
            if(usePanning && Input.GetKey(panningKey) && MouseAxis != Vector2.zero)
            {
                Vector3 desiredMove = new Vector3(-MouseAxis.x, 0, -MouseAxis.y);

                desiredMove *= panningSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = transform.InverseTransformDirection(desiredMove);

                transform.Translate(desiredMove, Space.Self);
            }
        }
			
		/// <summary>
		/// zoom camera
		/// </summary>
		private void Zoom() {

			// Get scroll value from keyboard / mousewheel
			if(useScrollwheelZooming)
				// Ratio of about 10 times required to obtain similar zoom as with keyboard
				zoomPos +=  10 * ScrollWheel * Time.deltaTime * scrollWheelZoomingSensitivity * (invertScrollDirection ? 1 : -1);			
			if (useKeyboardZooming)
				zoomPos += ZoomDirection * Time.deltaTime * keyboardZoomingSensitivity;

			// Get resulting position with scroll applied
			zoomPos *= Mathf.Clamp01(heightDampening);
			scrolling.transform.position = transform.position;
			scrolling.transform.rotation = transform.rotation;
			scrolling.transform.Translate(Vector3.back * zoomPos);

			// Apply scroll if within height limits
			if (scrolling.transform.position.y > minHeight && scrolling.transform.position.y < maxHeight)
				transform.Translate(Vector3.back * zoomPos);
		}


        /// <summary>
        /// rotate camera
        /// </summary>
        private void Rotation()
        {
			if(useKeyboardRotation)
                transform.Rotate(Vector3.up, RotationDirection * Time.deltaTime * rotationSpeed, Space.World);			

            // Movement only if using mouse rotation and key is held down
            // Or if both the mouse rotation and secondary keys are held down if using secondary key option
            if (useMouseRotation && Input.GetKey(mouseRotationKey) &&
                (!useMouseRotationSecondaryKey || 
                 Input.GetKey(mouseRotationSecondaryKey) || Input.GetKey(mouseRotationSecondaryKeyOther)))
            {				
                IsRotatingWithMouse = true;

                // Movement only along horizontal axis (locked vertical)
                transform.Rotate(Vector3.up, -MouseAxis.x * Time.deltaTime * mouseRotationSpeed, Space.World);
            }
            else
            {
                IsRotatingWithMouse = false;
            }
        }


        public bool IsRotatingWithMouse { get; set; }


        /// <summary>
        /// follow targetif target != null
        /// </summary>
        private void FollowTarget()
        {
            Vector3 targetPos = new Vector3(targetFollow.position.x, transform.position.y, targetFollow.position.z) + targetOffset;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * followingSpeed);
        }

        /// <summary>
        /// limit camera position
        /// </summary>
        private void LimitPosition()
        {
            if (!limitMap)
                return;
                
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, -limitX, limitX),
                transform.position.y,
                Mathf.Clamp(transform.position.z, -limitY, limitY));
        }

        /// <summary>
        /// set the target
        /// </summary>
        /// <param name="target"></param>
        public void SetTarget(Transform target)
        {
            targetFollow = target;
        }

        /// <summary>
        /// reset the target (target is set to null)
        /// </summary>
        public void ResetTarget()
        {
            targetFollow = null;
        }

        /// <summary>
        /// calculate distance to ground
        /// </summary>
        /// <returns></returns>
        private float DistanceToGround()
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, groundMask.value))
                return (hit.point - transform.position).magnitude;

            return 0f;
        }

        #endregion
    }
}