using UnityEngine;
using System.Collections;

namespace UI
{
	public class PanelManager : MonoBehaviour 
	{
		public GameObject LinkedBuilding = null;
		public GameObject LinkedUnit = null;

		public bool Print(string whatever)
		{
			Debug.Log("BUTTON0 Clicked!");
			return false;
		}
	}
}