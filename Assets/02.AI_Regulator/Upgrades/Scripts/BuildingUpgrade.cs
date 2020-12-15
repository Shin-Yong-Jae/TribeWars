using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Building Upgrade script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))] //requires the Building component to be attached to the gameobject.
    public class BuildingUpgrade : Upgrade<Building>
    {
        public EffectObj upgradeEffect; //the upgrade effect object that is spawned when the building upgrades (at the buildings pos).
    }
}

