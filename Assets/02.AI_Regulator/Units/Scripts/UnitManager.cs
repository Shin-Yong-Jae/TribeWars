using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine
{
	public class UnitManager : MonoBehaviour {

		public static UnitManager Instance;

		[Header("Free Units:")]
		public Unit[] FreeUnits; //units that don't belong to any faction here.
		public Color FreeUnitSelectionColor = Color.black;

        [Header("Animations:")]
        public AnimatorOverrideController DefaultAnimController; //default override animation controller: used when there's no animation override controller assigned to a unit

        //a list of all units (alive):
        public static List<Unit> allUnits = new List<Unit>();

		void Awake ()
		{
			if (Instance == null) {
				Instance = this;
			} else if (Instance != this) {
				Destroy (gameObject);
			}
		}

        void OnEnable ()
		{
			CustomEvents.UnitConverted += OnUnitConverted;
		}

		void OnDisable ()
		{
			CustomEvents.UnitConverted -= OnUnitConverted;
		}

		//Converter events:
		void OnUnitConverted (Unit Unit, Unit TargetUnit)
		{
			if (TargetUnit != null) {
				//if the unit is selected:
				if (GameManager.Instance.SelectionMgr.SelectedUnits.Contains (TargetUnit)) {
					//if this is the faction that the unit got converted to or this is the only unit that the player is selecting:
					if (TargetUnit.FactionID == GameManager.PlayerFactionID || GameManager.Instance.SelectionMgr.SelectedUnits.Count == 1) {
						//simply re-select the player:
						GameManager.Instance.SelectionMgr.SelectUnit (TargetUnit, false);
					} else { //this means this is the faction that the target belonged to before and that player is selecting multiple units including the newly converted target unit
						//deselect it:
						GameManager.Instance.SelectionMgr.DeselectUnit (TargetUnit);
					}
				}
			}
		}

        public static Unit CreateUnit (Unit unitPrefab, Vector3 spawnPosition, int factionID, Building createdBy, bool freeUnit = false)
        {
            if (GameManager.MultiplayerGame == false) //single player game 
                return UnitManager.CreateUnitLocal(unitPrefab, spawnPosition, factionID, createdBy, freeUnit); //directly create new unit instance
            else //if this is a multiplayer
            {
                //if it's a MP game, then ask the server to spawn the unit.
                //send input action to the input manager
                NetworkInput NewInputAction = new NetworkInput
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.unit,
                    initialPosition = spawnPosition, //when upgrade == true, then set to 1. if not set to 0
                    value = freeUnit == true ? 1 : 0
                };
                InputManager.SendInput(NewInputAction, unitPrefab.gameObject, (createdBy == null) ? null : createdBy.gameObject); //send to input manager

                return null;
            }
        }

        public static Unit CreateUnitLocal (Unit unitPrefab, Vector3 spawnPosition, int factionID, Building createdBy, bool freeUnit)
        {
            //only if the prefab is valid:
            if (unitPrefab == null)
                return null;

            // create the new unit:
            unitPrefab.gameObject.GetComponent<NavMeshAgent>().enabled = false; //disable this component before spawning the unit as it might place the unit in an unwanted position when spawned
            Unit newUnit = Instantiate(unitPrefab.gameObject, spawnPosition, unitPrefab.transform.rotation).GetComponent<Unit>();

            newUnit.SetFree(freeUnit);

            //set the unit faction ID.
            if (freeUnit == false)
                newUnit.FactionID = factionID;

            newUnit.Creator = createdBy; //unit is created by which building? (if there's any)

            newUnit.gameObject.GetComponent<NavMeshAgent>().enabled = true; //enable the nav mesh agent component for the newly created unit

            return newUnit;
        }
    }
}