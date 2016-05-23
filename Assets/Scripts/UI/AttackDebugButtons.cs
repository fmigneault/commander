using UnityEngine;
using System.Collections;

public class AttackDebugButtons : MonoBehaviour {

	public GameObject Target;
	public GameObject[] AttackUnits;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public void SetAttackTarget() 
	{
		
		foreach (var u in AttackUnits)
		{
			u.GetComponent<Units.TankManager>().AimingTarget = Target;
		}
	}


	public void ResetAttackTarget()
	{
		foreach (var u in AttackUnits)
		{
			u.GetComponent<Units.TankManager>().AimingTarget = null;
		}
	}
}
