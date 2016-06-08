using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI 
{
	[RequireComponent(typeof(Text))]
	public class DisplayDebugText : MonoBehaviour 
	{
		public Camera CameraDetails;
		private RTS_Cam.RTS_Camera camRTS;
		private Text debugText;

		// Use this for initialization
		void Start () 
		{
			debugText = gameObject.GetComponent<Text>();
			camRTS = CameraDetails.GetComponent<RTS_Cam.RTS_Camera>();
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
			return string.Format("T: {0}\nR: {1}\nXZ limits: (-{2},{3})\nY limits: ({4},{5})",
				CameraDetails.transform.position.ToString(), CameraDetails.transform.rotation.eulerAngles.ToString(),
				camRTS.limitX, camRTS.limitY, camRTS.minHeight, camRTS.maxHeight);	
		}


		private string FormatInputInformation() 
		{
			return string.Format("Horizontal: {0}\nVertical: {1}", Input.GetAxis(camRTS.horizontalAxis), Input.GetAxis(camRTS.verticalAxis));	
		}
	}
}
