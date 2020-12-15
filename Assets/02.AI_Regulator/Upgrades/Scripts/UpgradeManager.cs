using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Upgrade Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager instance; //only a single instance of this component is allowed per map.

        //holds unit upgrade tasks info that need to be synced when a new task launcher is added
        private struct UpgradedUnitTask
        {
            public int factionID;
            public string upgradedUnitCode;
            public Unit targetUnitPrefab;
            public UnitUpgrade.NewTaskInfo newTaskInfo;
        }
        private List<UpgradedUnitTask> upgradedUnitTasks = new List<UpgradedUnitTask>();

        GameManager gameMgr;
        BuildingPlacement buildingPlacement;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(instance);
        }

        private void Start()
        {
            gameMgr = GameManager.Instance; //get the active instance of the game manager.
            buildingPlacement = BuildingPlacement.instance;
        }

        private void OnEnable()
        {
            //start listening to custom events:
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
        }

        private void OnDisable()
        {
            //stop listening to custom events:
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
        }

        //method caleld when a building upgrade is launched
        public void LaunchUpgrade (BuildingUpgrade buildingUpgrade, int upgradeID)
        {
            string instanceCode = buildingUpgrade.GetSource().GetCode();
            int factionID = buildingUpgrade.GetSource().FactionID;

            if(upgradeID >= buildingUpgrade.target.Length || upgradeID < 0) //if upgrade ID is invalid then print an error
            {
                Debug.LogError("[Upgrade Manager] Invalid building upgrade target ID!");
                return;
            }

            //make sure the target prefab is valid and that the faction ID is valid as well:
            if (buildingUpgrade == null || factionID < 0)
                return;

            //trigger the upgrade event:
            CustomEvents.instance.OnBuildingUpgraded(buildingUpgrade);

            //go through the spawned buildings list of the faction:
            List<Building> spawnedBuildings = gameMgr.Factions[factionID].FactionMgr.Buildings;

            int i = 0;
            int buildingsCount = spawnedBuildings.Count;

            //will hold the building instances to be upgraded
            List<Building> instancesToUpgrade = new List<Building>();

            foreach(Building b in spawnedBuildings)
            {
                //if this building matches the instance to be upgraded
                if (b.GetCode() == instanceCode)
                {
                    instancesToUpgrade.Add(b);
                }
            }

            //go through the instances to upgrade list and upgrade them
            foreach(Building b in instancesToUpgrade)
                UpgradeBuildingInstance(b, buildingUpgrade.target[upgradeID], factionID, gameMgr.Factions[factionID].playerControlled ? buildingUpgrade.upgradeEffect : null);

            //is this the local player's faction:
            if (gameMgr.Factions[factionID].playerControlled == true)
            {
                //search for the building instance inside the buildings list that the player is able to place.
                for (i = 0; i < buildingPlacement.AllBuildings.Count; i++)
                    //if the code matches the instance's code then replace building:
                    if (buildingPlacement.AllBuildings[i].GetCode() == instanceCode)
                        buildingPlacement.AllBuildings[i] = buildingUpgrade.target[upgradeID]; //replace it
            }
            //& if the faction belongs is NPC:
            else if(gameMgr.Factions[factionID].GetNPCMgrIns() != null)
                LaunchNPCUpgrade(gameMgr.Factions[factionID].GetNPCMgrIns(), buildingUpgrade, upgradeID);

            //trigger upgrades?
            LaunchTriggerUpgrades(buildingUpgrade.triggerUnitUpgrades, buildingUpgrade.triggerBuildingUpgrades);
        }

        //a method called that configures NPC faction components in case of a building upgrade:
        private void LaunchNPCUpgrade (NPCManager npcMgrIns, BuildingUpgrade buildingUpgrade, int upgradeID)
        {
            //we need access to the NPC Building Creator in order to find the active regulator instance that manages the building type to be upgraded:
            NPCBuildingCreator buildingCreator_NPC = npcMgrIns.buildingCreator_NPC;

            NPCBuildingRegulator buildingRegulator = npcMgrIns.GetBuildingRegulatorAsset(buildingUpgrade.GetSource()); //will hold the building's regulator that is supposed to be upgraded.
            NPCBuildingRegulator targetBuildingRegulator = npcMgrIns.GetBuildingRegulatorAsset(buildingUpgrade.target[upgradeID]); ; //will hold the target building's regulator.

            //we expect both above regulators to be valid:
            if (buildingRegulator == null)
            {
                Debug.LogError("[Upgrade Manager] Can not find a valid NPC Building Regulator for the upgrade source.");
                return;
            }
            if (targetBuildingRegulator == null)
            {
                Debug.LogError("[Upgrade Manager] Can not find a valid NPC Building Regulator for the upgrade target.");
                return;
            }

            //destroy the old building regulator
            buildingCreator_NPC.DestroyActiveRegulator(buildingRegulator);

            //if the building to be upgraded was either, the main population building or the main center building
            //then we'll update that as well.
            if (buildingRegulator == npcMgrIns.populationManager_NPC.populationBuilding)
            {
                npcMgrIns.populationManager_NPC.populationBuilding = targetBuildingRegulator;
                npcMgrIns.populationManager_NPC.ActivatePopulationBuilding(); //activate the new population building regulator.
            }
            if (buildingRegulator == npcMgrIns.territoryManager_NPC.centerRegulator)
            {
                npcMgrIns.territoryManager_NPC.centerRegulator = targetBuildingRegulator; //assign new regulator for the center building
                npcMgrIns.territoryManager_NPC.ActivateCenterRegulator(); //activate the new regulator.
            }

            //activate the new regulator:
            buildingCreator_NPC.ActivateBuildingRegulator(targetBuildingRegulator);

        }

        //a method that upgrades a building's instance locally
        public void UpgradeBuildingInstance (Building buildingInstance, Building targetBuilding, int factionID, EffectObj upgradeEffect)
        {
            //if there's a valid upgrade effect assigned:
            if (upgradeEffect != null)
                //show the upgrade effect for the player:
                EffectObjPool.SpawnEffectObj(upgradeEffect, buildingInstance.transform.position, Quaternion.identity);

            Vector3 buildingPos = buildingInstance.transform.position; //get its position.
            Border buildingCenter = buildingInstance.CurrentCenter; //get the building's center.
            buildingInstance.HealthComp.DestroyFactionEntity(true); //destroy the building's instance.

            //create upgraded instance of the building
            BuildingManager.CreatePlacedInstance(targetBuilding, buildingPos, targetBuilding.transform.rotation.eulerAngles.y, buildingCenter, factionID, true);
        }

        //a method called to launch a unit upgrade:
        public void LaunchUpgrade (UnitUpgrade unitUpgrade, int upgradeID)
        {
            string instanceCode = unitUpgrade.GetSource().GetCode();
            int factionID = unitUpgrade.GetSource().FactionID;

            if (upgradeID >= unitUpgrade.target.Length || upgradeID < 0) //if upgrade ID is invalid then print an error
            {
                Debug.LogError("[Upgrade Manager] Invalid unit upgrade target ID!");
                return;
            }

            //make sure the target prefab is valid and that the faction ID is valid as well:
            if (unitUpgrade == null || factionID < 0)
                return;

            //trigger the upgrade event:
            CustomEvents.instance.OnUnitUpgraded(unitUpgrade);

            //search for a task that creates the unit to upgrade inside the task launchers
            List<TaskLauncher> taskLaunchers = gameMgr.Factions[factionID].FactionMgr.TaskLaunchers;

            //go through the active task launchers:
            foreach(TaskLauncher tl in taskLaunchers)
            {
                //and sync the upgraded tasks
                UpdateUnitCreationTask(tl, instanceCode, unitUpgrade.target[upgradeID], unitUpgrade.newTaskInfo);
            }

            //register the upgraded unit creation task:
            UpgradedUnitTask uut = new UpgradedUnitTask()
            {
                factionID = factionID,
                upgradedUnitCode = instanceCode,
                targetUnitPrefab = unitUpgrade.target[upgradeID],
                newTaskInfo = unitUpgrade.newTaskInfo
            };
            //add it to the list:
            upgradedUnitTasks.Add(uut);

            //if the faction belongs is NPC:
            if (gameMgr.Factions[factionID].GetNPCMgrIns() != null)
            {
                LaunchNPCUpgrade(gameMgr.Factions[factionID].GetNPCMgrIns(), unitUpgrade, upgradeID);
            }

            //trigger upgrades?
            LaunchTriggerUpgrades(unitUpgrade.triggerUnitUpgrades, unitUpgrade.triggerBuildingUpgrades);
        }

        //a method called that configures NPC faction components in case of a unit upgrade:
        private void LaunchNPCUpgrade(NPCManager npcMgrIns, UnitUpgrade unitUpgrade, int upgradeID)
        {
            //we need access to the NPC Unit Creator in order to find the active regulator instance that manages the unit type to be upgraded:
            NPCUnitCreator unitCreator_NPC = npcMgrIns.unitCreator_NPC;

            NPCUnitRegulator unitRegulator = npcMgrIns.GetUnitRegulatorAsset(unitUpgrade.GetSource()); //will hold the unit's regulator that is supposed to be upgraded.
            NPCUnitRegulator targetUnitRegulator = npcMgrIns.GetUnitRegulatorAsset(unitUpgrade.target[upgradeID]); ; //will hold the target unit's regulator

            //we expect both above regulators to be valid:
            if (unitRegulator == null)
            {
                Debug.LogError("[Upgrade Manager] Can not find a valid NPC Unit Regulator for the upgrade source.");
                return;
            }
            if (targetUnitRegulator == null)
            {
                Debug.LogError("[Upgrade Manager] Can not find a valid NPC Unit Regulator for the upgrade target.");
                return;
            }

            //destroy the old building regulator
            unitCreator_NPC.DestroyActiveRegulator(unitRegulator);

            //if the unit to be upgraded was either, the main builder, collector or one of the army units
            //then we'll update that as well.
            if (unitRegulator == npcMgrIns.buildingConstructor_NPC.builderRegulator)
            {
                npcMgrIns.buildingConstructor_NPC.builderRegulator = unitRegulator;
                npcMgrIns.buildingConstructor_NPC.ActivateBuilderRegulator(); //activate the new unit regulator
            }
            if (unitRegulator == npcMgrIns.resourceCollector_NPC.collectorRegulator)
            {
                npcMgrIns.resourceCollector_NPC.collectorRegulator = unitRegulator;
                npcMgrIns.resourceCollector_NPC.ActivateCollectorRegulator(); //activate the new unit regulator
            }
            if (npcMgrIns.armyCreator_NPC.armyUnitRegulators.Contains(unitRegulator)) //is the unit to upgrade an army unit?
            {
                npcMgrIns.armyCreator_NPC.armyUnitRegulators.Remove(unitRegulator); //remove old regulator from list
                npcMgrIns.armyCreator_NPC.armyUnitRegulators.Add(targetUnitRegulator); //add new regulator asset
                npcMgrIns.armyCreator_NPC.ActivateArmyUnitRegulators(); //activate army regulators.
            }

            //activate the new regulator:
            unitCreator_NPC.ActivateUnitRegulator(targetUnitRegulator);
        }

        //trigger unit/building upgrades locally:
        private void LaunchTriggerUpgrades(UnitUpgrade[] unitUpgrades, BuildingUpgrade[] buildingUpgrades)
        {
            foreach (UnitUpgrade uu in unitUpgrades)
                LaunchUpgrade(uu, 0); //will trigger the upgrade for the first upgrade target only!
            foreach (BuildingUpgrade bu in buildingUpgrades)
                LaunchUpgrade(bu, 0);
        }

        //called whenever a task launcher is added
        private void OnTaskLauncherAdded (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            SyncUnitCreationTasks(taskLauncher); //sync the upgraded unit creation tasks
        }

        //sync all upgraded unit creation tasks for a task launcher:
        private void SyncUnitCreationTasks (TaskLauncher taskLauncher)
        {
            //go through the registered upgraded unit tasks
            foreach(UpgradedUnitTask uut in upgradedUnitTasks)
            {
                //if this task launcher belongs to the faction ID that has the upgraded unit creation task:
                if (uut.factionID == taskLauncher.FactionID)
                {
                    //sync the unit creation tasks.
                    UpdateUnitCreationTask(taskLauncher, uut.upgradedUnitCode, uut.targetUnitPrefab, uut.newTaskInfo);
                }
            }
        }

        //update an upgraded unit creation task's info:
        private void UpdateUnitCreationTask (TaskLauncher taskLauncher, string upgradedUnitCode, Unit targetUnitPrefab, UnitUpgrade.NewTaskInfo newTaskInfo)
        {
            //go through the tasks:
            foreach (TaskLauncher.TasksVars tv in taskLauncher.TasksList)
            {
                //does the current task create the unit that is getting upgraded
                if (tv.TaskType == TaskManager.TaskTypes.CreateUnit && tv.UnitCreationSettings.Prefabs[0].GetCode() == upgradedUnitCode)
                {
                    //update task info:
                    tv.UnitCreationSettings.Prefabs.Clear();
                    tv.UnitCreationSettings.Prefabs.Add(targetUnitPrefab);

                    tv.Description = newTaskInfo.description;
                    tv.TaskIcon = newTaskInfo.icon;
                    tv.ReloadTime = newTaskInfo.reloadTime;
                    tv.RequiredResources = newTaskInfo.newResources.Clone() as ResourceManager.Resources[];
                }
            }
        }
    }
}

