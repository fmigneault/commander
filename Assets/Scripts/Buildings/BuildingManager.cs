// Display debugging/logging info on console
#define OUTPUT_DEBUG

using UnityEngine;
using UnityEngine.UI;
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

		#if OUTPUT_DEBUG
		public Text DebugLogText;
		#endif

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
			if (unit != null && unit.GetComponent<UnitManager>() != null)
			{
				if (ProductionQueue.Count < ProductionQueueSize)
				{
					ProduceUnitAfterDelay(unit);
					return true;
				}
			}
			return false;
		}


		private void ProduceUnitAfterDelay(GameObject unit=null, bool isCallback=false)
		{
			// Update production queue if a new unit has to be added
			if (unit != null) ProductionQueue.Enqueue(unit);

			// If a unit is already under production or all units have been produced, return
			// Function 'ProduceCurrentUnit' manages the callback when the current unit finishes production
			if ((ProductionQueue.Count > 1 && !isCallback) || ProductionQueue.Count == 0) return;

			#if OUTPUT_DEBUG
			DisplayQueuedUnits();
			#endif

			// Start production of the first unit in queue
			StartCoroutine(ProduceCurrentUnit((GameObject)ProductionQueue.Peek()));		
		}


		private IEnumerator ProduceCurrentUnit(GameObject unit)
		{
			// Wait for the unit's production delay before creating it
			float delay = unit.GetComponent<UnitManager>().ProductionDelay;
			float timestampComplete = Time.time + delay;
			while (Time.time < timestampComplete)
			{
				CurrentDelay = timestampComplete - Time.time;
				yield return new WaitForSeconds(delay);
			}

			// Create the unit when the delay expires
			//    Use 'peek' to get current unit, only dequeue when unit is completely produced
			//    This avoids a timing and a null reference problem that occured when adding a unit to the production 
			//    queue while the current unit was removed from queue, while not having finished the production delay
			var producedUnit = ProductionQueue.Peek();
			yield return CreateProducedUnit((GameObject)producedUnit);
			ProductionQueue.Dequeue();

			// Callback to start the next unit's production if applicable
			ProduceUnitAfterDelay(isCallback: true);
		}


		private IEnumerator CreateProducedUnit(GameObject unit)
		{			
			if (Door != null)
			{
				// Wait for the door to open completely
				CurrentDoorStatus = DoorStatus.OPENING;
				yield return new WaitUntil(() => CurrentDoorStatus == DoorStatus.IDLE);

				// Move the unit from spawn position to exit position
				var createdUnit = (GameObject)Instantiate(unit, SpawnPosition.position, SpawnPosition.rotation);
				yield return MoveCreatedUnitToExit(createdUnit);

				// Wait for the door to close completely
				CurrentDoorStatus = DoorStatus.CLOSING;
				yield return new WaitUntil(() => CurrentDoorStatus == DoorStatus.IDLE);						
			}
		}


		private IEnumerator MoveCreatedUnitToExit(GameObject unit)
		{
			float movingSpeed = unit.GetComponent<UnitManager>().MovingSpeed;
			while (unit.transform.position != ExitPosition.position)
			{				
				unit.transform.position = Vector3.MoveTowards(unit.transform.position, ExitPosition.position,
															  movingSpeed * Time.deltaTime);
				yield return null;
			}
		}
			

		private void UpdateDoorPosition()
		{
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

			#if OUTPUT_DEBUG
			string debug = string.Format("Door: {0} {1}", doorPosition, CurrentDoorStatus);
			Debug.Log(debug);
			if (DebugLogText != null) DebugLogText.text = debug;
			#endif
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
					if (DebugLogText != null) DebugLogText.text = queueInfo;
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