using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Unit Upgrade script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Unit))] //requires the Unit component to be attached to the gameobject.
    public class UnitUpgrade: Upgrade<Unit>
    {
        [System.Serializable]
        //the following attributes will replace the attributes in the tasks where the unit to upgrade can be created:
        public struct NewTaskInfo
        {
            public string description;
            public Sprite icon;
            public float reloadTime;
            public ResourceManager.Resources[] newResources;
        }
        public NewTaskInfo newTaskInfo = new NewTaskInfo();
    }
}

