using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* APC script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class APC : MonoBehaviour {

        private FactionEntity factionEntity; //the main unit/building component

        [Header("Adding Units"),SerializeField]
        private Transform interactionPosition; //position where units get in/off the APC
        public Vector3 GetInteractionPosition () { return interactionPosition.position; }

        [SerializeField]
        private bool acceptAllUnits = true; //allow all units to get in the APC?
        [SerializeField]
        private bool acceptUnitsInList = true; //this determines how the APC will handle the below list if the above bool is set to false, accept units defined there or deny them?
        [SerializeField]
        private List<string> codesList = new List<string>(); //a list of the unit codes that are allowed/not allowed to get in the APC

        [SerializeField]
		private int capacity = 4; //max amount of units to be stored in the APC at the same time
        public int GetCapacity () { return capacity; }
		private List<Unit> storedUnits = new List<Unit>(); //holds the current units stored in the APC unit.
        public bool IsEmpty () { return storedUnits.Count == 0; }
        public bool IsFull () { return storedUnits.Count >= capacity; }
        public int GetCount () { return storedUnits.Count;  } //how many units are currently stored in the APC?
        public Unit GetStoredUnit (int id) { //get a reference to a unit that's stored inside the APC
            if (id >= 0 && id < storedUnits.Count)
                return storedUnits[id];
            return null;
        }

        [SerializeField]
        private AudioClip addUnitAudio = null; //audio clip played when a unit gets in the APC

        [Header("Ejecting Units"), SerializeField]
        private bool canEjectSingleUnit = true; //can the player eject single units?
        [SerializeField]
        private int ejectSingleUnitTaskCategory = 0; //the category ID of ejecting a single unit task. 

        [SerializeField]
        private bool canEjectAllUnits = true; //true when the APC is allowed to eject units all at once
        [SerializeField]
        private int ejectAllUnitsTaskCategory = 1; //the category ID of ejecting all units at once
        [SerializeField]
        private Sprite ejectAllUnitsIcon = null; //The icon of the task of ejecting all units at once
        public Sprite GetEjectAllUnitsIcon () { return ejectAllUnitsIcon; }

        public bool CanEject (bool allUnits) { return allUnits == true ? canEjectAllUnits : canEjectSingleUnit; }
        public int GetTaskCategory (bool allUnits) { return allUnits == true ? ejectAllUnitsTaskCategory : ejectSingleUnitTaskCategory;  }

        [SerializeField]
        private AudioClip ejectUnitAudio = null; //audio clip played when a unit is removed from the APC

        [SerializeField]
        private bool ejectOnDestroy = true; //if true, all units will be released on destroy, if false, all contained units will be destroyed.

        [Header("Calling Units"),SerializeField]
        private bool canCallUnits = true; //can the APC call units to get them inside?
        public bool CanCallUnits () { return canCallUnits; }
        [SerializeField]
        private int callUnitsTaskCategory = 0; //the category ID of calling all units task
        public int GetCallUnitsTaskCategory() { return callUnitsTaskCategory; }
        [SerializeField]
        private float callUnitsRange = 20.0f; //the range at which units will be called to get into the APC
        [SerializeField]
        private Sprite callUnitsIcon = null; //The task's icon that will eject all the contained units when launched.
        public Sprite GetCallUnitsIcon () { return callUnitsIcon; }
        [SerializeField]
        private bool callIdleOnly = false; //call units that are in idle mode only
        [SerializeField]
        private bool callAttackUnits = false; //call units that have an attack component?

        [SerializeField]
        private AudioClip callUnitsAudio = null; //audio clip played when the APC is calling units

        private void Awake ()
        {
            factionEntity = GetComponent<FactionEntity>();
        }

		void Start ()
		{
            if (interactionPosition == null) //no interaction position is assigned
                interactionPosition = transform; //assign the interaction position to the faction entity's position
		}

        //a method to check whether a unit can be added to this APC or not
        public bool CanAddUnit (Unit unit)
        {
            return (acceptAllUnits == true
                || ((acceptUnitsInList == true && codesList.Contains( unit.GetCode() ) )
                || (acceptUnitsInList == false && !codesList.Contains( unit.GetCode() ) ) ) );
        }

		//method called to add a unit to the APC:
		public bool Add (Unit unit)
		{
            string errorMessage = "";
            bool error = false;
            if (IsFull()) //max capacity reached
            {
                errorMessage = "APC reached maximum capacity!";
                error = true;
            }
            else if (unit.APCComp != null || !CanAddUnit(unit))
            { //unit is not allowed in this APC
                errorMessage = "Unit is not allowed to enter the APC!";
                error = true;
            }

            if (error == true)
            {
                if (factionEntity.FactionID == GameManager.PlayerFactionID) //if this is the player's faction
                    UIManager.instance.ShowPlayerMessage(errorMessage, UIManager.MessageTypes.Error);
                return false;
            }

            unit.gameObject.SetActive(false); //hide unit object
            storedUnits.Add(unit); //add it to the stored units list
            unit.transform.SetParent(transform, true); //make it a child object of the APC object (so that it moves with the APC)

            SelectionManager.instance.DeselectUnit(unit); //deselect unit in case it was selected

            AudioManager.PlayAudio(gameObject, addUnitAudio, false);

            if (SelectionManager.instance.IsFactionEntitySelected(factionEntity)) //if the APC is selected
                UIManager.instance.UpdateTaskPanel(); //update task panel

            CustomEvents.instance.OnAPCAddUnit(this, unit); //trigger custom event

            return true;
		}

        //a method called to eject all units
        public void EjectAll (bool destroyed)
        {
            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                EjectAllLocal(destroyed);
            else if (GameManager.Instance.IsLocalPlayer(gameObject)) //multiplayer game and this is the APC's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.APC,
                    targetMode = (byte)InputMode.APCEjectAll,
                    value = destroyed == true ? 1 : 0
                };

                InputManager.SendInput(newInput, gameObject, null); //send input to input manager
            }
        }

        //a method called to eject all units
        public void EjectAllLocal(bool destroyed)
        {
            while(storedUnits.Count > 0) //go through all stored units and remove them
                EjectLocal(storedUnits[0], destroyed);
        }

        //a method called to eject one unit
        public void Eject (Unit unit, bool destroyed)
        {
            if (unit == null || storedUnits.Contains(unit) == false) //invalid unit or unit that's not stored here?
                return;

            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                EjectLocal(unit, destroyed);
            else if (GameManager.Instance.IsLocalPlayer(gameObject)) //multiplayer game and this is the APC's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.APC,
                    targetMode = (byte)InputMode.APCEject,
                    value = destroyed == true ? 1 : 0
                };

                InputManager.SendInput(newInput, gameObject, unit.gameObject); //send input to input manager
            }
        }

        //a method called to eject one unit locally
        public void EjectLocal(Unit unit, bool destroyed)
        {
            if (unit == null || storedUnits.Contains(unit) == false) //invalid unit or unit that's not stored here?
                return;

            unit.transform.SetParent(null, true); //APC is no longer the parent of the unit object
            storedUnits.Remove(unit); //remove unit from the list
            unit.gameObject.SetActive(true); //activate object

            AudioManager.PlayAudio(gameObject, ejectUnitAudio, false);

            if (SelectionManager.instance.IsFactionEntitySelected(factionEntity)) //if the APC is selected
                UIManager.instance.UpdateTaskPanel(); //update task panel

            CustomEvents.instance.OnAPCRemoveUnit(this, unit); //trigger custom event

            //if the APC is marked as destroyed and units are supposed to be destroyed as well
            if (destroyed == true && ejectOnDestroy == false)
                unit.HealthComp.DestroyFactionEntity(false); //destroy unit

        }

		//method called when the APC requests nearby units to enter.
		public void CallUnits ()
		{
			int i = 0; //counter
			AudioManager.PlayAudio(gameObject, callUnitsAudio, false); //play the call for units audio.

			while (i < factionEntity.FactionMgr.Units.Count && storedUnits.Count < capacity) { //go through the faction's units while still making sure that there is enough space for units to get in

                Unit u = factionEntity.FactionMgr.Units[i];
                float distance = Vector3.Distance(transform.position, u.transform.position);

                //if the APC calls idle units only and the current unit is not idle or the APC doesn't call attack units and the current unit has an attack component
                if (CanAddUnit(u) == false || (callIdleOnly == true && u.IsIdle() == false) || (callAttackUnits == false && u.AttackComp != null))
                {
                    i++;
                    continue; //move to next unit in loop
                }

                //the target unit can't be another APC, it must be active, alive and inside the calling range
                if (u.APCComp == null && u.gameObject.activeInHierarchy == true && u.HealthComp.IsDead() == false && distance <= callUnitsRange)
                    MovementManager.instance.Move(u, interactionPosition.position, 0.0f, gameObject, InputMode.APC, false); //move unit towards APC

                i++;
			}
		}
	}
}