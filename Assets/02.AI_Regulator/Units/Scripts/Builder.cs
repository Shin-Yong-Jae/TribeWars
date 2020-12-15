using UnityEngine;
using System.Collections;

/* Builder script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Builder : UnitComponent<Building> {

        [SerializeField]
        private bool constructFreeBuildings = false; //can the builder construct free buildings that do not belong to any faction?
        public bool CanConstructFreeBuilding () { return constructFreeBuildings; }

        [SerializeField]
		private int healthPerSecond = 5; //amount of health that the building will receive per second

        [SerializeField]
		private AudioClip[] constructionAudio = new AudioClip[0]; //played when the unit is constructing a building

        public override void Start()
        {
            base.Start();

            healthPerSecond = (int)(healthPerSecond * GameManager.Instance.GetSpeedModifier()); //get the speed modifier and set the health per second accordinly
        }

        //update component if the builder has a target
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            return base.OnActiveUpdate(
                1.0f,
                UnitAnimState.building,
                constructionAudio,
                target.HealthComp.CurrHealth >= target.HealthComp.MaxHealth);
        }

        //a method that is called when the builder arrives at the target building to construct
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            CustomEvents.instance.OnUnitStartBuilding(unit, target); //trigger custom event.
        }

        //a method that is called when the builder achieved progress in construction
        protected override void OnInProgress()
        {
            base.OnInProgress();

            target.HealthComp.AddHealth(healthPerSecond, unit); //add health points to the building.
        }

        //update component if the builder doesn't have a target
        protected override void OnInactiveUpdate()
        {
            base.OnInactiveUpdate();
        }

        //a method called when builder searches for a target:
        protected override void OnTargetSearch()
        {
            base.OnTargetSearch();

            foreach (Building b in unit.FactionMgr.Buildings) //go through the faction's building list as long as the unit doesn't have a target
                if (b.gameObject.activeInHierarchy == true && b.HealthComp.CurrHealth < b.HealthComp.MaxHealth && Vector3.Distance(b.transform.position, transform.position) < autoBehavior.GetSearchRange()) //if the current building doesn't have max health and is in the defined range
                {
                    SetTarget(b); //set as new target
                    break; //leave loop
                }
        }

        //a method that stops the builder from constructing
        public override bool Stop ()
        {
            Building lastTarget = target;

            if (base.Stop() == false)
                return false;

            CustomEvents.instance.OnUnitStopBuilding(unit, lastTarget); //trigger custom event

            lastTarget.WorkerMgr.Remove(unit);//remove the unit from the worker manager

            if (SelectionManager.instance.IsBuildingSelected(lastTarget)) //if the target building was selected
                UIManager.instance.UpdateBuildingUI(lastTarget);

            return true;
        }

        //a method that sets the target building to construct
        public override void SetTarget (Building newTarget, InputMode targetMode = InputMode.none)
        {
            base.SetTarget(newTarget, InputMode.builder);
        }

        //a method that sets the target building to construct locally
        public override void SetTargetLocal (Building newTarget)
		{
			if (newTarget == null || target == newTarget) //if the new target is invalid or it's already the builder's target
				return; //do not proceed

            //Does the new target need construction (doesn't have max health) and it has a slot in its worker manager
            if (newTarget.HealthComp.CurrHealth < newTarget.HealthComp.MaxHealth && newTarget.WorkerMgr.currWorkers < newTarget.WorkerMgr.GetAvailableSlots())
            {
                Stop(); //stop constructing the current building

                //set new target
				inProgress = false;
				target = newTarget;

                MovementManager.instance.PrepareMove(unit, target.WorkerMgr.Add(unit), target.GetRadius(), target.gameObject, InputMode.building, false); //move the unit towards the target building
            }
		}
	}
}