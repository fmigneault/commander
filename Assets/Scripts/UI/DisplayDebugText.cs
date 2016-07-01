using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using Cameras;

namespace UI 
{
	[RequireComponent(typeof(Text))]
	public class DisplayDebugText : MonoBehaviour 
	{
		public Camera CameraDetails;
        private RTS_CameraManager cameraRTS;
		private Text debugText;

		// Use this for initialization
		void Start () 
		{
			debugText = gameObject.GetComponent<Text>();
            cameraRTS = CameraDetails.GetComponent<RTS_CameraManager>();
		}

		// Update is called once per frame
		void Update () 
		{
			if (debugText != null && debugText.enabled)
			{
				debugText.text = string.Format("{0}\n\n{1}", FormatCameraInformation(), FormatInputInformation());
			}			
		}


		private string FormatCameraInformation() 
		{
            return string.Format("T: {0}\nR: {1}\nXZ limits: (-{2},{3})\nY limits: ({4},{5})\nRotate enabled: {6}",
				CameraDetails.transform.position.ToString(), CameraDetails.transform.rotation.eulerAngles.ToString(),
                cameraRTS.limitX, cameraRTS.limitY, cameraRTS.minHeight, cameraRTS.maxHeight, cameraRTS.useMouseRotation);	
		}


		private string FormatInputInformation() 
		{
            return string.Format("Horizontal: {0}\nVertical: {1}", Input.GetAxis(cameraRTS.horizontalAxis), Input.GetAxis(cameraRTS.verticalAxis));	
		}
	}
}
