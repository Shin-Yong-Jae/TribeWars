using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Upgrade script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public abstract class Upgrade<T> : MonoBehaviour
    {
        protected T source; //the source instance (unit/building) that will be upgraded.
        public T GetSource() { return gameObject.GetComponent<T>(); }

        public T[] target; //the target prefabs (units/buildings) that this entity can upgrade to. The choice of which target to ugprade is determined in the task launcher's settings.

        //a list of unit/building upgrades that will be triggered when this upgrade is launched.
        public UnitUpgrade[] triggerUnitUpgrades = new UnitUpgrade[0];
        public BuildingUpgrade[] triggerBuildingUpgrades = new BuildingUpgrade[0];
    }
}
