using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Healer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Healer : UnitComponent<Unit>
    {

        [SerializeField]
        private float stoppingDistance = 5.0f; //when assigned a target unit, this is the stopping distance that the healer will have
        [SerializeField]
        private float maxDistance = 7.0f; //the maximum distance between the healer and the target unit to heal.
        public float GetStoppingDistance() { return stoppingDistance; }

        [SerializeField]
        public int healthPerSecond = 5; //amount of health to give the target unit per second.

        [SerializeField]
        private AudioClip[] healingAudio = null;

        //a method that stops the unit from healing
        public override bool Stop()
        {
            Unit lastTarget = target;

            if (base.Stop() == false)
                return false;

            CustomEvents.instance.OnUnitStopHealing(unit, lastTarget); //trigger custom event

            if (SelectionManager.instance.IsUnitSelected(lastTarget)) //if the target unit was selected
                UIManager.instance.UpdateUnitUI(lastTarget);

            return true;
        }

        //update component if the healer has a target unit
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (base.OnActiveUpdate(
                1.0f,
                UnitAnimState.healing,
                healingAudio,
                //if target has max health the healer and the target don't have the same faction or the target is outside the max allowed range for healing -> cancel job
                target.HealthComp.CurrHealth >= target.HealthComp.MaxHealth ||
                target.FactionID != unit.FactionID ||
                (Vector3.Distance(transform.position, target.transform.position) > maxDistance && inProgress == true)
                ) == false)
                return false;

            return true;
        }

        //a method that is called when the healer arrives at the target unit to heal
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            CustomEvents.instance.OnUnitStartHealing(unit, target); //trigger custom event.
        }

        //a method that is called when the healer achieved progress in healing
        protected override void OnInProgress()
        {
            base.OnInProgress();

            target.HealthComp.AddHealth(healthPerSecond, unit); //add health points to the target unit
        }

        //update component when the healer doesn't have a target unit
        protected override void OnInactiveUpdate()
        {
            base.OnInactiveUpdate();
        }

        //a method called when the healer searches for a target:
        protected override void OnTargetSearch()
        {
            base.OnTargetSearch();

            foreach (Unit u in unit.FactionMgr.Units) //go through the faction's units list and look for a target unit
                if (u.gameObject.activeInHierarchy == true && u.HealthComp.CurrHealth < u.HealthComp.MaxHealth && Vector3.Distance(u.transform.position, transform.position) < autoBehavior.GetSearchRange()) //if the current unit doesn't have max health and it's inside the search range
                {
                    SetTarget(u); //set as new target
                    break; //leave loop
                }
        }

        //a method that sets the target unit to heal
        public override void SetTarget(Unit newTarget, InputMode targetMode = InputMode.none)
        {
            base.SetTarget(newTarget, InputMode.heal);
        }

        //a method that sets the healing target locally
        public override void SetTargetLocal(Unit newTarget)
        {
            if (newTarget == null || newTarget == target)
                return;

            if (newTarget.FactionID == unit.FactionID && newTarget.HealthComp.CurrHealth < newTarget.HealthComp.MaxHealth) //Make sure the new target has the same faction ID as the healer and it doesn't have max health
            {
                Stop(); //stop healing the current unit

                //set new target
                inProgress = false;
                target = newTarget;

                MovementManager.instance.Move(unit, target.transform.position, stoppingDistance, target.gameObject, InputMode.unit, false); //move the unit towards the target unit
            }
        }
    }
}