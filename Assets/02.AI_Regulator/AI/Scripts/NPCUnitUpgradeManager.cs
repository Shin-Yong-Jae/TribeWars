using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Unit Upgrade Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCUnitUpgradeManager : NPCUpgradeManager<Unit>
    {
        private void OnEnable()
        {
            //start listening to the delegate events
            CustomEvents.UnitUpgraded += OnUnitUpgraded;
        }

        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.UnitUpgraded += OnUnitUpgraded;
        }

        //called whenever a unit is upgraded:
        private void OnUnitUpgraded(Upgrade<Unit> upgrade)
        {
            //does the unit belongs to this NPC faction?
            if (upgrade.GetSource().FactionID == factionMgr.FactionID)
            {
                UpdateUpgradeTasks(null, false, upgrade);
            }
        }

        //implementation of the is upgrade match method.
        protected override bool IsUpgradeMatch(TaskLauncher taskLauncher, int taskID, Upgrade<Unit> unitUpgrade)
        {
            if (unitUpgrade == null || taskLauncher == null)
                return false;

            //true only if the source unit's code match
            return taskLauncher.TasksList[taskID].unitUpgrade.GetSource().GetCode() == unitUpgrade.GetSource().GetCode();
        }
    }
}
