using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/* Resource Collector script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class ResourceCollector : UnitComponent<Resource> {

        //drop off attributes:
        [SerializeField]
		private int maxCapacity = 7; //the maximum quantity of each resource that the unit can hold before having to drop it off at the closet building that allows him to do so, only if auto collection is disabled

        public class DropOffResource //this class holds the current amount of each resource the unit is holding:
		{
			public int CurrAmount { set; get; }
            private string name;

            public void Init (string name)
            {
                this.name = name;
                CurrAmount = 0;
            }

            //a method that adds the collected resource here to the faction's resources
            public void AddResource(int factionID)
            {
                ResourceManager.instance.AddResource(factionID, name, CurrAmount);
                CurrAmount = 0;
            }
        }
        private DropOffResource[] dropOffResources;

        private Building dropOffBuilding; //where does the unit drop resources at?
        public enum DropOffStatus {inactive, ready, active, done, goingBack};
        private DropOffStatus dropOffStatus;
        public bool IsDroppingOff () { return dropOffStatus == DropOffStatus.active; }

        [SerializeField]
        private GameObject dropOffObject = null; //child object of the unit, activated when the player is dropping off resources.
        [SerializeField]
        private AnimatorOverrideController dropOffOverrideController = null; //the animator override controller that is active when the unit is dropping off resources

		[System.Serializable]
		public class CollectionObject
		{
            [SerializeField]
            private ResourceTypeInfo resourceType = null; //the resource type that this collection object is associated with
            public string GetResourceName () { return resourceType.Name; }

            [SerializeField]
            private GameObject obj = null; //child object of the unit
            public GameObject GetObject () { return obj; }

            [SerializeField]
            private AnimatorOverrideController animatorOverrideController = null; //when collecting resources, the unit can have different collection animation for each resource type
            public AnimatorOverrideController GetOverrideController() { return animatorOverrideController; }
        }
        [SerializeField]
        private CollectionObject[] collectionObjects = new CollectionObject[0];
        private Dictionary<string, CollectionObject> collectionObjectsDic = new Dictionary<string, CollectionObject>();

        public override void Start () {
            if(ResourceManager.instance.AutoCollect == false) //if auto resource collection is off and units must drop off resources
            {
                dropOffResources = new DropOffResource[ResourceManager.instance.ResourcesInfo.Length];
                for (int i = 0; i < dropOffResources.Length; i++)
                {
                    dropOffResources[i] = new DropOffResource(); //initialize the drop off resources
                    dropOffResources[i].Init(ResourceManager.instance.ResourcesInfo[i].TypeInfo.Name);
                }

                if (dropOffObject) //if there's a drop off model has been assigned
                    dropOffObject.SetActive(false); //hide it initially

                //for fast access time, add all the collection objects info to a dictionary
                collectionObjectsDic.Clear();
                foreach (CollectionObject co in collectionObjects)
                    collectionObjectsDic.Add(co.GetResourceName(), co);
            }
		}

        //a method that cancels the resource collection
        public override bool Stop()
        {
            Resource lastTarget = target;

            if (base.Stop() == false || dropOffStatus == DropOffStatus.active)
                return false;

            CustomEvents.instance.OnUnitStopCollecting(unit, lastTarget); //trigger custom event

            lastTarget.WorkerMgr.Remove (unit);//remove the unit from the worker manager

            inProgressObject = null;

            if (dropOffObject != null) //hide the drop off object
                dropOffObject.SetActive(false);

            if (SelectionManager.instance.IsResourceSelected(lastTarget)) //if the target resource was selected
                UIManager.instance.UpdateResourceUI(lastTarget);

            //reset collection settings
            CancelDropOff();

            unit.ResetAnimatorOverrideController(); //set the animator override controller of the collector back to the default one

            return true;
        }

        //update component if the collector has a target
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (base.OnActiveUpdate(
                target.GetCollectOneUnitDuration(),
                UnitAnimState.collecting,
                ResourceManager.instance.ResourcesInfo[target.ID].TypeInfo.CollectionAudio.ToArray(),
                target.IsEmpty(), //target resource must not be empty
                (ResourceManager.instance.AutoCollect == true || (dropOffStatus == DropOffStatus.goingBack || dropOffStatus == DropOffStatus.inactive)), //auto collection must be enabled or the unit must currently not be dropping off resources
                dropOffStatus != DropOffStatus.active //in order to be able to collect resources, unit must not be dropping off resources
                ) == false)
                return false;

            if (dropOffBuilding != null && dropOffStatus == DropOffStatus.active && unit.MovementComp.DestinationReached) //unit is currently dropping off resources while having a valid drop off building
            {
                DropOff();
                inProgress = false; //unit is no longer collecting => needs to go back to the resource to collect

                if (dropOffObject != null) //hide the collection object
                    dropOffObject.SetActive(false);
            }

            return true;
        }

        //a method that is called when the collector arrives at the target resource to collect
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            if (dropOffStatus != DropOffStatus.goingBack) //only if the unit wasn't coming back after a drop off -> first time collecting
            {
                UpdateDropOffResources(target.ID, 0); //check if the unit needs to drop off resources
                CustomEvents.instance.OnUnitStartCollecting(unit, target); //trigger custom event
            }

            //if the unit was coming back after a drop off, then that is done & unit is no longer dropping off resources
            if (dropOffStatus == DropOffStatus.goingBack)
                CancelDropOff();

            if (collectionObjectsDic.TryGetValue(target.GetResourceName(), out CollectionObject collectionObject))
                unit.SetAnimatorOverrideController(collectionObject.GetOverrideController()); //update the runtime animator controller
        }

        //a method that is called when the builder achieved progress in collection
        protected override void OnInProgress()
        {
            base.OnInProgress();

            if (target.TreasureComp != null) //if the resource has a treasure component
            {
                target.DestroyResource(unit); //destroy the resource which will trigger opening the treasure
            }
            else if (target.IsEmpty() == false) //we're dealing with a normal resource that's not empty yet
            {
                target.AddAmount(-1, unit); //take one unit of the resource
            }
        }

        //update component if the collector doesn't have a target
        protected override void OnInactiveUpdate()
        {
            base.OnInactiveUpdate();
        }

        //a method called when collector searches for a target:
        protected override void OnTargetSearch()
        {
            base.OnTargetSearch();

            foreach (Resource r in ResourceManager.instance.AllResources) //go through all resources list
                if (r != null && r.gameObject.activeInHierarchy == true && r.FactionID == unit.FactionID && r.IsEmpty() == false && Vector3.Distance(r.transform.position, transform.position) < autoBehavior.GetSearchRange()) //if the resource is in the collector's faction territory, it's not empty yet and it's inside the search range
                {
                    SetTarget(r); //set as new target
                    break; //leave loop
                }
        }

        //a method that sets the target resource to collect
        public override void SetTarget(Resource newTarget, InputMode targetMode = InputMode.none)
        {
            base.SetTarget(newTarget, InputMode.collect);
        }

        //a method that sets the target resource to collect locally
        public override void SetTargetLocal (Resource newTarget)
		{
            if (newTarget.IsEmpty() || newTarget == null || (target == newTarget && dropOffStatus != DropOffStatus.done)) //if the new target is invalid or it's already the collector's target (if the collector is not coming back after dropping off resources)
                return; //do not proceed

            if(target != newTarget) //if the resource wasn't being collected by this collector:
            {
                if (newTarget.WorkerMgr.currWorkers < newTarget.WorkerMgr.GetAvailableSlots() && (newTarget.CanCollectOutsideBorder() == true || newTarget.FactionID == unit.FactionID)) //does the new target has empty slots in its worker manager? and can this be collected outside the border or this under the collector's territory
                {
                    CancelDropOff(); //cancel drop off if it was pending

                    Stop(); //stop collecting from the last resource (if there was one).

                    //set new target
                    inProgress = false;
                    target = newTarget;

                    inProgressObject = null; //initially set to nothing

                    if (collectionObjectsDic.TryGetValue(target.GetResourceName(), out CollectionObject collectionObject))
                        inProgressObject = collectionObject.GetObject(); //update the current collection object


                    //Search for the nearest drop off building if we are not automatically collecting:
                    if (ResourceManager.instance.AutoCollect == false)
                        UpdateDropOffBuilding();

                    //Move the unit to the resource and add the collector to the workers list in the worker manager
                    MovementManager.instance.PrepareMove(unit, target.WorkerMgr.Add(unit), target.GetRadius(), target.gameObject, InputMode.resource, false);

                    CustomEvents.instance.OnUnitCollectionOrder(unit, target); //trigger custom event
                }
            }
            else if(dropOffStatus == DropOffStatus.done) //collector is going back to the resource after a drop off
            {
                MovementManager.instance.PrepareMove(unit, target.WorkerMgr.GetWorkerPos(unit.LastWorkerPosID), target.GetRadius(), target.gameObject, InputMode.resource, false);
                dropOffStatus = DropOffStatus.goingBack;
            }
		}

		//a method that finds the closest drop off building to an input position
		public void UpdateDropOffBuilding ()
		{
            if (target == null) //if there's no target resource
                return;

            Building closestBuilding = null;
            float minDistance = 0.0f;

            foreach(Building b in unit.FactionMgr.DropOffBuildings) //go through all faction's drop off buildings
                if (b.IsBuilt == true && b.DropOffComp.CanDrop(target.GetResourceName())) //if this drop off building is completely built and it accepts the target resource
                    if (closestBuilding == null || Vector3.Distance(target.transform.position, b.transform.position) < minDistance) //is this the closest drop off building?
                    {
                        closestBuilding = b;
                        minDistance = Vector3.Distance(target.transform.position, b.transform.position);
                    }

            if (closestBuilding == null && unit.FactionID == GameManager.PlayerFactionID) //no drop off building has been found
                UIManager.instance.ShowPlayerMessage("No suitable drop off building is on the map!", UIManager.MessageTypes.Error);

            dropOffBuilding = closestBuilding; //update the drop off building

            if (dropOffStatus == DropOffStatus.ready && dropOffBuilding != null) //if the unit is already waiting to drop off resources and a valid drop off destination has been assigned
                SendToDropOff(); //send the unit to drop off building then
        }

        //a method that sends the unit to drop off resources at the drop off building
        public void SendToDropOff ()
        {
            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                SendToDropOffLocal();
            else if (GameManager.Instance.IsLocalPlayer(gameObject)) //multiplayer game and this is the collector's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.dropoff,
                    initialPosition = transform.position
                };

                InputManager.SendInput(newInput, gameObject, dropOffBuilding.gameObject);
            }
        }

        //a method that sends the unit to drop off resources at the drop off building locally
        public void SendToDropOffLocal ()
        {
            if (dropOffBuilding == null && unit.FactionID == GameManager.PlayerFactionID) //no drop off building and this is the player's faction
            {
                UIManager.instance.ShowPlayerMessage("Unit can't drop off resources as there's no suitable drop off building on the map!", UIManager.MessageTypes.Error);
                return;
            }

            AudioManager.StopAudio(gameObject); //stop the collection audio clip.

            dropOffStatus = DropOffStatus.active;
            unit.SetAnimatorOverrideController(dropOffOverrideController); //enable the drop off override controller.

            MovementManager.instance.PrepareMove(unit, dropOffBuilding.DropOffComp.GetDropOffPosition(), dropOffBuilding.DropOffComp.GetDropOffRadius(), dropOffBuilding.gameObject, InputMode.building, false);
        }

        //drop off resources at the drop off building
        public void DropOff ()
		{
            foreach (DropOffResource dor in dropOffResources) //go through the drop off resources
                dor.AddResource(unit.FactionID);

            //make the unit go back to the resource he's collecting from:
            dropOffStatus = DropOffStatus.done;
            SetTarget(target);

            unit.ResetAnimatorOverrideController(); //in case the animator override controller has been modified, reset it
		}

        //a method that updates the drop off resources that the collector is holding
        public void UpdateDropOffResources(int resourceID, int value)
        {
            dropOffResources[resourceID].CurrAmount += value; //update the current amount of this drop off resource

            if (dropOffResources[resourceID].CurrAmount >= maxCapacity) //if the maximum capacity has been reached
            {
                dropOffStatus = DropOffStatus.ready; //the collector is now in drop off mode
                unit.SetAnimState(UnitAnimState.idle); //move to idle state
                SendToDropOff(); //send the unit to drop off resources
                AudioManager.StopAudio(gameObject); //stop the collection audio.

                if (inProgressObject != null)
                    inProgressObject.SetActive(false); //hide the collection object
                
                if (dropOffObject != null) //activate the drop off object
                    dropOffObject.SetActive(true);
            }
        }

        //a method that cancels drop off:
        public void CancelDropOff ()
        {
            dropOffStatus = DropOffStatus.inactive;
        }
    }
}