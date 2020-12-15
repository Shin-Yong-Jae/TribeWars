using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/* Input Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager instance;

        [SerializeField]
        private float snapDistance = 0.5f; //max distance allowed between a unit's current position and its initial position in an input before it gets snapped.

        private List<GameObject> spawnablePrefabs = new List<GameObject>(); //what prefabs can this component actually create in a multiplayer game?
        private List<GameObject> spawnedObjects = new List<GameObject>(); //the objects spawned by this component are saved in here.

        //Mirror:
#if RTSENGINE_MIRROR
        public static NetworkFactionManager_Mirror FactionManager_Mirror { set; get; } //the local player's network faction manager component
#endif

        private void Awake()
        {
            if (instance == null) //make sure there's only one instance of the Input Manager in the scene:
                instance = this;
            else if (instance != this)
                Destroy(this);

            spawnedObjects.Clear(); //the spawned objects list is empty by default.
            spawnablePrefabs.Clear();

#if RTSENGINE_MIRROR
            if(GameManager.Instance.NetworkManager_Mirror)
                spawnablePrefabs = GameManager.Instance.NetworkManager_Mirror.spawnablePrefabs;
#endif
        }

        void Start()
        {
            if (GameManager.MultiplayerGame == false) //if this is not a multiplayer game then destroy this component
            {
                Destroy(this);
                return;
            }
        }

        //this is the only way to communicate between objects in the scene and multiplayer faction managers.
        public static void SendInput(NetworkInput newInput, GameObject sourceObject, GameObject targetObject)
        {
            if (sourceObject) //if there's a source object
            {
                if (newInput.sourceMode == (byte)InputMode.create) //if we're creating an object, then look in the spawnable prefabs list
                    newInput.sourceID = InputManager.instance.spawnablePrefabs.IndexOf(sourceObject); //get the index of the prefab from the spawnable prefabs list as the source ID
                else //for the rest of input source modes, get the ID from the spawned objects list
                    newInput.sourceID = InputManager.instance.spawnedObjects.IndexOf(sourceObject);
            }
            else
                newInput.sourceID = -1; //no source object

            if (targetObject) //if there's a valid target object
                newInput.targetID = InputManager.instance.spawnedObjects.IndexOf(targetObject); //get its index from the spawn objects and set it as the target ID.
            else
                newInput.targetID = -1; //no target object assigned

            newInput.factionID = GameManager.PlayerFactionID; //source of the input is the local player's faction

#if RTSENGINE_MIRROR
            if (FactionManager_Mirror != null) //network faction manager hasn't been assigned yet
                FactionManager_Mirror.CmdSendInput(newInput); //send the input
#endif
        }

        //a method called to execute commands (collected inputs) sent by the host/server
        public void LaunchCommand(NetworkInput command)
        {
            switch ((InputMode)command.sourceMode)
            {
                case InputMode.spawnFaction:
                    OnSpawnFactionCommand(command);
                    break;
                case InputMode.create:
                    OnCreateCommand(command);
                    break;
                case InputMode.destroy:
                    OnDestroyCommand(command);
                    break;
                case InputMode.factionEntity:
                    OnFactionEntityCommand(command);
                    break;
                case InputMode.unitGroup:
                    OnUnitGroupMovementCommand(command);
                    break;
                case InputMode.unit:
                    OnUnitCommand(command);
                    break;
                case InputMode.building:
                    OnBuildingCommand(command);
                    break;
                case InputMode.resource:
                    OnResourceCommand(command);
                    break;
                case InputMode.APC:
                    OnAPCCommand(command);
                    break;
                case InputMode.customCommand:
                    CustomEvents.instance.OnCustomCommand(command);
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input source mode!");
                    break;
            }
        }
    
        //execute a command that spawns a faction
        private void OnSpawnFactionCommand(NetworkInput command)
        {
            //if no faction type info has been assigned or the capital building hasn't been assigned, then we can't spawn a capital building for this player
            if (GameManager.Instance.Factions[command.factionID].TypeInfo == null || GameManager.Instance.Factions[command.factionID].TypeInfo.capitalBuilding == null)
            {
                Debug.LogError("[Input Manager] The Faction Type Info or the Capital Building hasn't been assigned for player with faction ID: " + command.factionID);
                return;
            }

            Building capitalBuilding = BuildingManager.CreatePlacedInstanceLocal(
                GameManager.Instance.Factions[command.factionID].TypeInfo.capitalBuilding
                , command.initialPosition, command.targetPosition.y, null, command.factionID, true); //spawn the capital building.

            capitalBuilding.FactionCapital = true; //mark as a capital faction

            if (command.factionID == GameManager.PlayerFactionID) //if this is the local player? (owner of this capital building)
            {
                //Set the player's initial camera position (looking at the faction's capital building)
                CameraMovement.instance.LookAt(capitalBuilding.transform.position);
                CameraMovement.instance.SetMiniMapCursorPos(capitalBuilding.transform.position);
            }

            spawnedObjects.Add(capitalBuilding.gameObject); //add the new object to the list

            if (command.factionID == GameManager.HostFactionID) //we'll use the spawn of the host's faction capital to go through free units, free buildings and resources to register them.
            {
                //register add all free buildings and units and resources:
                foreach (Unit freeUnit in UnitManager.Instance.FreeUnits)
                    spawnedObjects.Add(freeUnit.gameObject);
                foreach (Building freeBuilding in BuildingPlacement.instance.FreeBuildings)
                    spawnedObjects.Add(freeBuilding.gameObject);
                foreach (Resource resource in ResourceManager.instance.AllResources)
                    spawnedObjects.Add(resource.gameObject);
            }
        }

        //execute a command that creates a faction entity (unit/building):
        private void OnCreateCommand(NetworkInput command)
        {
            GameObject prefab = spawnablePrefabs[command.sourceID]; //the prefab to spawn.
            GameObject newInstance = null;

            switch ((InputMode)command.targetMode)
            {
                case InputMode.unit:

                    Building creator = (command.targetID >= 0) ? spawnedObjects[command.targetID].GetComponent<Building>() : null;
                    newInstance = UnitManager.CreateUnitLocal(prefab.GetComponent<Unit>(), command.initialPosition, command.factionID, creator, command.value == 1).gameObject;

                    break;
                case InputMode.building:

                    /*
             * 0 -> PlacedByDefault = false & Capital = false
             * 1 -> PlacedByDefault = true & Capital = false
             * 2 -> PlacedByDefault = false & Capital = true
             * 3 -> PlacedByDefault = true & Capital = true
             * */
                    //determine whether the building will be placed by default or if it's a capital building.
                    bool placedByDefault = (command.value == 1 || command.value == 3) ? true : false;
                    bool isCapital = (command.value == 2 || command.value == 3) ? true : false;

                    Border center = (command.targetID >= 0) ? spawnedObjects[command.targetID].GetComponent<Border>() : null; //get the building's center border component

                    newInstance = BuildingManager.CreatePlacedInstanceLocal(prefab.GetComponent<Building>(), command.initialPosition, 0.0f, center, command.factionID, placedByDefault).gameObject;
                    newInstance.GetComponent<Building>().FactionCapital = isCapital;

                    break;

                case InputMode.resource:
                    newInstance = ResourceManager.CreateResourceLocal(prefab.GetComponent<Resource>(), command.initialPosition).gameObject;
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input target mode for creation command.");
                    break;
            }

            if (newInstance != null) //if a new instance of a unit/building/resource is created:
                spawnedObjects.Add(newInstance);
        }

        //execute a command that destroys a faction entity or a whole faction
        private void OnDestroyCommand(NetworkInput command)
        {
            switch ((InputMode)command.targetMode)
            {
                case InputMode.factionEntity: //destroying a unit
                    spawnedObjects[command.sourceID].GetComponent<FactionEntityHealth>().DestroyFactionEntityLocal(command.value == 1 ? true : false);
                    break;
                case InputMode.resource: //destroy a resource and providing the last resource collector as a parameter (if it exists).
                    spawnedObjects[command.sourceID].GetComponent<Resource>().DestroyResourceLocal(command.targetID >= 0 ? spawnedObjects[command.targetID].GetComponent<Unit>() : null);
                    break;
                case InputMode.faction:

                    GameManager.Instance.OnFactionDefeatedLocal(command.value); //the command.value holds the faction ID of the faction to destroy

                    if (GameManager.PlayerFactionID == GameManager.HostFactionID) //if this is the host
                    {
#if RTSENGINE_MIRROR
                        FactionManager_Mirror.OnFactionDefeated(command.value); //to mark the defeated player as disconnected.
#endif
                    }
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input target mode for destroy command.");
                    break;
            }
        }

        //execute a unit related command
        public void OnFactionEntityCommand(NetworkInput command)
        {
            FactionEntity sourceFactionEntity = spawnedObjects[command.sourceID].GetComponent<FactionEntity>(); //get the source faction entity
            GameObject targetObject = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID].gameObject : null; //get the target obj

            switch ((InputMode)command.targetMode)
            {
                case InputMode.health: //adding health

                    sourceFactionEntity.EntityHealthComp.AddHealthLocal(command.value, (targetObject == null) ? null : targetObject.GetComponent<FactionEntity>());
                    break;
                case InputMode.multipleAttack:  //switching attack types.

                    sourceFactionEntity.MultipleAttackMgr.EnableAttack(command.value);
                    break;
            }
        }

            //execute a unit group movement command
            public void OnUnitGroupMovementCommand(NetworkInput command)
        {
            List<Unit> unitList = StringToUnitList(command.groupSourceID); //get the units list

            if (unitList.Count > 0) //if there's actual units in the list
            {
                if (command.targetMode == (byte)InputMode.attack) //if the target mode is attack -> make the unit group launch an attack on the target.
                {
                    FactionEntity targetEntity = spawnedObjects[command.targetID].GetComponent<FactionEntity>(); //get the faction entity component of the target object
                    MovementManager.instance.PrepareMove(unitList, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, false, true, (MovementManager.AttackModes)command.value, targetEntity);
                }
                else //target movement type can be none, portal, APC, etc...
                    MovementManager.instance.PrepareMove(unitList, command.targetPosition, command.value, null, (InputMode)command.targetMode, true);
            }
        }

        //execute a unit related command
        public void OnUnitCommand(NetworkInput command)
        {
            Unit sourceUnit = spawnedObjects[command.sourceID].GetComponent<Unit>(); //get the source unit
            GameObject targetObject = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID].gameObject : null; //get the target obj

            if (Vector3.Distance(sourceUnit.transform.position, command.initialPosition) > snapDistance) //snap distance if the unit's current position has moved too far from the specified initial position
                sourceUnit.transform.position = command.initialPosition; //snap the unit's position

            if (command.targetMode == (byte)InputMode.movement) //no target mode -> movement
                MovementManager.instance.PrepareMove(sourceUnit, command.targetPosition, command.value, null, InputMode.movement, false);
            else if(targetObject != null) //if this is not a movement nor a health command and there's a valid target object.
            {
                switch ((InputMode)command.targetMode)
                {
                    case InputMode.APC: //targetting a APC

                        //move to target APC
                        MovementManager.instance.PrepareMove(sourceUnit, command.targetPosition, command.value, targetObject, InputMode.APC, false);
                        break;

                    case InputMode.heal: //healing the target

                        sourceUnit.HealerComp.SetTargetLocal(targetObject.GetComponent<Unit>()); //heal target unit
                        break;

                    case InputMode.convertOrder: //ordering the unit to convert

                        sourceUnit.ConverterComp.SetTargetLocal(targetObject.GetComponent<Unit>());
                        break;

                    case InputMode.convert: //unit is getting converted

                        sourceUnit.ConvertLocal(targetObject.GetComponent<Unit>());
                        break;

                    case InputMode.builder: //targetting a building

                        sourceUnit.BuilderComp.SetTargetLocal(targetObject.GetComponent<Building>());
                        break;

                    case InputMode.collect: //collecting a resource

                        sourceUnit.CollectorComp.SetTargetLocal(targetObject.GetComponent<Resource>());
                        break;

                    case InputMode.dropoff: //dropping off a resource

                        sourceUnit.CollectorComp.SendToDropOffLocal();
                        break;

                    case InputMode.portal: //moving to a portal

                        MovementManager.instance.PrepareMove(sourceUnit, command.targetPosition, command.value, targetObject, InputMode.portal, false);
                        break;

                    case InputMode.attack: //attacking the target object

                        FactionEntity targetEntity = targetObject.GetComponent<FactionEntity>(); //get the faction entity component of the target object
                        MovementManager.instance.PrepareMove(sourceUnit, targetEntity.transform.position, targetEntity.GetRadius(), targetEntity.gameObject, InputMode.attack, false, true, (MovementManager.AttackModes)command.value, targetEntity);
                        break;

                    case InputMode.multipleAttack:  //switching attack types.

                        sourceUnit.MultipleAttackMgr.EnableAttack(command.value);
                        break;

                    case InputMode.unitEscape:
                        sourceUnit.EscapeComp.TriggerLocal(command.targetPosition);
                        break;
                }
            }
        }

        //execute a building related command:
        private void OnBuildingCommand(NetworkInput command)
        {
            Building sourceBuilding = spawnedObjects[command.sourceID].GetComponent<Building>(); //get the source building
            GameObject targetObject = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID].gameObject : null; //get the target obj

            if (targetObject == null) //no target object assigned? 
                return; //do not proceed.

            switch ((InputMode)command.targetMode)
            {
                case InputMode.attack: //attacking a target

                    sourceBuilding.AttackComp.SetTargetLocal(targetObject.GetComponent<FactionEntity>());
                    break;
            }
        }

        //execute a resource related command:
        private void OnResourceCommand(NetworkInput command)
        {
            Resource sourceResource = spawnedObjects[command.sourceID].GetComponent<Resource>(); //get the source resource
            GameObject targetObject = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID].gameObject : null; //get the target obj

            if (targetObject == null) //no target object assigned? 
                return; //do not proceed.

            switch ((InputMode)command.targetMode)
            {
                case InputMode.health: //adding/removing an amount from the resource

                    sourceResource.AddAmountLocal(command.value, targetObject.GetComponent<Unit>());
                    break;
            }
        }

        //execute a APC related command:
        private void OnAPCCommand(NetworkInput command)
        {
            APC sourceAPC = spawnedObjects[command.sourceID].GetComponent<APC>(); //get the source resource

            switch ((InputMode)command.targetMode)
            {
                case InputMode.APCEjectAll: //ejecting all units from the APC

                    sourceAPC.EjectAllLocal(command.value == 1);
                    break;

                case InputMode.APCEject:

                    Unit targetUnit = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID].GetComponent<Unit>() : null; //get the target unit
                    sourceAPC.Eject(targetUnit, command.value == 1);
                    break;


            }
        }

        //convert a unit list into a string.
        public static string UnitListToString(List<Unit> unitList)
        {
            string resultString = "";
            foreach (Unit unit in unitList) //go through the unit list
                //get the ID of each unit in the list and add it to the string
                resultString += InputManager.instance.spawnedObjects.IndexOf(unit.gameObject).ToString() + ",";

            return resultString.TrimEnd(','); //trim the last ',' and voila.
        }

        //convert a string containing the IDs of units into a unit list:
        public static List<Unit> StringToUnitList(string inputString)
        {
            List<Unit> unitList = new List<Unit>();

            string[] unitIndexes = inputString.Split(','); //get the unit indexes into a string array
            foreach (string index in unitIndexes) //go through all the indexes
                unitList.Add(InputManager.instance.spawnedObjects[Int32.Parse(index)].GetComponent<Unit>()); //add the unit that matches the index to the list

            return unitList;
        }
    }
}