using UnityEngine;
using System.Collections;

namespace Units
{
	public class UnitManager : MonoBehaviour 
	{
		// General parameters
		public string Name = "";
		public bool isGroundUnit = false;
		public bool isAirUnit = false;

		// Movement parameters
		public float MovingSpeed = 0;
		public float RotationSpeed = 0;

		// Attack parameters
		public bool CanAttackGround = false;
		public bool CanAttackAir = false;
		public float MinAttackRange = 0;
		public float MaxAttackRange = 0;

		// Use this for initialization
		void Start () 
		{
		
		}
		
		// Update is called once per frame
		void Update () 
		{
		
		}
	}
}