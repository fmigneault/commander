using UnityEngine;
using System.Collections;
using RTS_Cam;

namespace Units
{
	[RequireComponent(typeof(RTS_Camera))]
	public class UnitSelector : MonoBehaviour 
	{
		public string[] SelectableTags;

		private float maxDistance;
		private Camera cam;

		void Start ()
		{
			cam = gameObject.GetComponent<RTS_Camera>().GetComponent<Camera>();
			maxDistance = gameObject.GetComponent<RTS_Camera>().maxHeight * 2;
		}
		

		void Update () 
		{
			if (Input.GetMouseButtonDown(0))
			{
				Select();
			}
		}


		private void Select()
		{
			if (SelectableTags != null)
			{				
				Ray ray = cam.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray.origin, ray.direction, out hit, maxDistance))
				{
					foreach (string tag in SelectableTags)
					{
						if (hit.transform.CompareTag(tag))
						{
							string hitName = hit.collider.gameObject.GetComponent<UnitManager>().Name;
							Debug.Log(string.Format("HIT: {0}, {1}", tag, hitName));
						}
					}					
				}	
			}
		}
	}
}
