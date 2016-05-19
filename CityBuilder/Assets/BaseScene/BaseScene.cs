using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BaseScene : MonoBehaviour 
{
    private void Start()
    {
               
    }

    private void SetXRotation(Transform t, float angle)
    {
        t.localEulerAngles = new Vector3(angle, t.localEulerAngles.y, t.localEulerAngles.z);
    }
}
