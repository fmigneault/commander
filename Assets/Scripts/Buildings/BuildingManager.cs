// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using System.Collections;
using Units;

namespace Buildings
{	
	public enum DoorStatus
	{
		OPENING,
		CLOSING,
		IDLE
	}


	public class BuildingManager : MonoBehaviour 
	{
		// General building information
		public string Name = "";

		// Parameters for unit creation animation
		public Transform SpawnPosition;
		public Transform ExitPosition;
		public GameObject Door = null;
		public float DoorSpeed = 0;
		public float DoorOpenedDeltaY = 0;
		public float DoorCloseWaitTime = 0;
		private DoorStatus CurrentDoorStatus = DoorStatus.IDLE;
		private Vector3 DoorOpenedPosition;
		private Vector3 DoorClosedPosition;

		// Parameters for unit production queue
		private const int ProductionQueueSize = 10;
		private readonly Queue ProductionQueue = new Queue(ProductionQueueSize);
		private float CurrentDelay = 0f;


		public void Start()
		{
			if (Door != null)
			{
				DoorClosedPosition = Door.transform.position;
				DoorOpenedPosition = DoorClosedPosition;
				DoorOpenedPosition.y += DoorOpenedDeltaY;
			}
		}


		public void Update()
		{			
			CurrentDelay -= Time.deltaTime;
			if (CurrentDelay < 0) CurrentDelay = 0;

			#if OUTPUT_DEBUG
			DisplayQueuedUnits();
			#endif

			if (Door != null) UpdateDoorPosition();
		}


		public bool AddUnitToProductionQueue(GameObject unit) 
		{
			#region DEBUG
			Debug.Log("START 'AddUnitToProductionQueue'");
			#endregion

			if (unit != null && unit.GetComponent<UnitManager>() != null)
			{
				if (ProductionQueue.Count < ProductionQueueSize)
				{
					#region DEBUG
					Debug.Log("QUEUE 'AddUnitToProductionQueue'");
					#endregion

					ProduceUnitAfterDelay(unit);
					return true;
				}
			}
			return false;
		}


		private void ProduceUnitAfterDelay(GameObject unit, bool isCallBack=false)
		{
			#region DEBUG
			Debug.Log("START 'ProduceUnitAfterDelay'");
			#endregion

			// Update production queue if a new unit has to be added
			if (unit != null) ProductionQueue.Enqueue(unit);

			// If a unit is already under production, return
			// The function will manage the callback itself when previous units are produced
			if (ProductionQueue.Count > 1 && !isCallBack) return;

			#region DEBUG
			Debug.Log("BEFORE COROUTINE 'ProduceUnitAfterDelay'");
			#endregion

			// Start the current unit's production
			StartCoroutine(ProduceCurrentUnit(unit));

			// Start the next unit's production when the current is complete
			ProduceUnitAfterDelay(null, true);
		}


		private IEnumerator ProduceCurrentUnit(GameObject unit)
		{
			#region DEBUG
			Debug.Log("START 'ProduceCurrentUnit'");
			#endregion

			// Wait for the unit's production delay before creating it
			float delay = unit.GetComponent<UnitManager>().ProductionDelay;
			CurrentDelay = delay;
			yield return new WaitForSeconds(delay);

			// Create the unit when the delay expires
			var producedUnit = ProductionQueue.Dequeue();
			StartCoroutine(CreateProducedUnit((GameObject)producedUnit));
		}


		private IEnumerator CreateProducedUnit(GameObject unit)
		{			
			if (Door != null)
			{
				#region DEBUG
				Debug.Log("START 'CreateProducedUnit'");
				#endregion

				// Wait for the door to open completely
				CurrentDoorStatus = DoorStatus.OPENING;
				yield return new WaitUntil(() => CurrentDoorStatus == DoorStatus.IDLE);

				#region DEBUG
				Debug.Log("UNIT CREATED");
				#endregion
				//Instantiate(unit);

				// Wait for the door to close completely
				CurrentDoorStatus = DoorStatus.CLOSING;
				yield return new WaitUntil(() => CurrentDoorStatus == DoorStatus.IDLE);
			}
		}
			

		public void UpdateDoorPosition()
		{
			#region DEBUG
			Debug.Log("START 'UpdateDoorPosition'");
			#endregion

			Vector3 doorPosition = Door.transform.position;
			switch (CurrentDoorStatus)
			{
				case DoorStatus.OPENING:
					doorPosition = Vector3.MoveTowards(doorPosition, DoorOpenedPosition, DoorSpeed * Time.deltaTime);
					if (doorPosition == DoorOpenedPosition) CurrentDoorStatus = DoorStatus.IDLE;
					break;
				case DoorStatus.CLOSING:
					doorPosition = Vector3.MoveTowards(doorPosition, DoorClosedPosition, DoorSpeed * Time.deltaTime);
					if (doorPosition == DoorClosedPosition) CurrentDoorStatus = DoorStatus.IDLE;
					break;
				default:
					return;
			}
			Door.transform.position = doorPosition;
		}


		#if OUTPUT_DEBUG
		public void DisplayQueuedUnits()
		{
			string queueInfo = "Queued: ";
			for (int i = 0; i < ProductionQueueSize; i++) 
			{
				if (i == ProductionQueue.Count)
				{
					queueInfo += string.Format("<END> | RemainTime: {0}", CurrentDelay);
					Debug.Log(queueInfo);
					return;
				}
				var unit = (GameObject)(ProductionQueue.ToArray()[i]);
				string unitName = unit.GetComponent<UnitManager>().Name;
				queueInfo += string.Format("({0}: {1})", i, unitName);
			}
		}
		#endif
	}
}