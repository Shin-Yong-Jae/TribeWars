using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Building Upgrade Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCBuildingUpgradeManager : NPCUpgradeManager<Building>
    {
        private void OnEnable()
        {
            //start listening to the delegate events
            CustomEvents.BuildingUpgraded += OnBuildingUpgraded;
        }

        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.BuildingUpgraded += OnBuildingUpgraded;
        }

        //called whenever a building is upgraded:
        private void OnBuildingUpgraded (Upgrade<Building> upgrade)
        {
            //does the building belongs to this NPC faction?
            if(upgrade.GetSource().FactionID == factionMgr.FactionID)
            {
                UpdateUpgradeTasks(null, false, upgrade);
            }
        }

        //implementation of the is upgrade match method.
        protected override bool IsUpgradeMatch(TaskLauncher taskLauncher, int taskID, Upgrade<Building> buildingUpgrade)
        {
            if (buildingUpgrade == null || taskLauncher == null)
                return false;

            //true only if the source building's code match
            return taskLauncher.TasksList[taskID].buildingUpgrade.GetSource().GetCode() == buildingUpgrade.GetSource().GetCode();
        }

    }
}
