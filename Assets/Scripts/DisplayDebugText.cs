using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DisplayDebugText : MonoBehaviour {

	public Camera cameraDetails;

	private Text debugTextCanvas;
	private Rect limits;
	private RTS_Cam.RTS_Camera camRTS;

	// Use this for initialization
	void Start () {
		debugTextCanvas = gameObject.GetComponent<Text>();
		camRTS = cameraDetails.GetComponent<RTS_Cam.RTS_Camera>();
	}
	
	// Update is called once per frame
	void Update () {
		if (debugTextCanvas.enabled)
		{
			debugTextCanvas.text = string.Format("{0}\n\n{1}", FormatCameraInformation(), FormatInputInformation());	
		}			
	}


	private string FormatCameraInformation() {
		return string.Format("T: {0}\nR: {1}\nXZ limits: (-{2},{3})\nY limits: ({4},{5})",
			cameraDetails.transform.position.ToString(), cameraDetails.transform.rotation.eulerAngles.ToString(),
			camRTS.limitX, camRTS.limitY, camRTS.minHeight, camRTS.maxHeight);	
	}


	private string FormatInputInformation() {
		return string.Format("Horizontal: {0}\nVertical: {1}", Input.GetAxis(camRTS.horizontalAxis), Input.GetAxis(camRTS.verticalAxis));	
	}
}
