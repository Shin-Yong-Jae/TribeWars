using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Unit Attack script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Unit))]
    public class UnitAttack : AttackEntity
    {
        Unit unit; //the building's main component

        [SerializeField]
        private string rangeTypeCode = "shortrange"; //input the attack range's type in this field (attack ranges can be defined in the attack manager).
        public AttackManager.UnitAttackRange RangeType { private set; get; }

        [SerializeField]
        private bool moveOnAttack = false; //is the unit allowed to trigger its attack while moving?
        [SerializeField]
        private float followDistance = 15.0f; //if the attack target's leaves the attack entity range (defined in the attack manager), then this is max distance between this and the target where the attack entity can follow its target before stopping the attack

        //animation related attributes:
        private bool canTriggerAnimation = true; //play the unit's attack animation?
        [SerializeField]
        private bool triggerAnimationInDelay = false; //true => the attack animation is triggered when the delay starts. if false, it will only be triggered when the attack is triggered 

        protected override void Awake()
        {
            base.Awake();
            unit = GetComponent<Unit>();
        }

        protected override void Start ()
        {
            base.Start();
            RangeType = AttackManager.instance.GetRangeType(rangeTypeCode);
        }

        //can the unit engage in an attack:
        public override bool CanEngage() //make sure the unit is not dead
        {
            return unit.HealthComp.IsDead() == false && coolDownTimer <= 0.0f;
        }

        //check whether the unit is in idle mode or not:
        public override bool IsIdle()
        {
            return unit.IsIdle();
        }

        //check whether the unit is in range of its target or not:
        public override bool IsTargetInRange()
        {
            if (Target == null) //no target?
                return false;

            if (moveOnAttack == true)
                return Vector3.Distance(transform.position, Target.transform.position) <= MovementManager.instance.GetStoppingDistance() + RangeType.GetStoppingDistance(Target.Type == FactionEntityTypes.unit) + ((moveOnAttack == true) ? RangeType.GetMoveOnAttackOffset() : 0);
            else
                return unit.MovementComp.DestinationReached;
        }

        //update in case the unit has an attack target:
        protected override void OnTargetUpdate()
        {
            if (Target.Type == FactionEntityTypes.unit) //if there's a target unit
            {
                //if this is not a AI unit defending a building and there's a target unit (not building) and it was already once inside the attack range of the target but the target moved away (distance is higher than the allowed follow distance) and the 
                if (SearchRangeCenter == null && wasInTargetRange == true && Vector3.Distance(transform.position, Target.transform.position) > Mathf.Max(followDistance, initialEngagementDistance))
                {
                    Stop(); //stop the attack.
                    return; //and do not proceed
                }

                //if the attack target unit changed its position before this unit reached it
                if (Vector3.Distance(lastTargetPosition, Target.transform.position) >= RangeType.GetUpdateMvtDistance())
                {
                    unit.MovementComp.DestinationReached = false; //Destination is not marked as reached anymore
                                                                  //launch the attack again so that the unit moves closer to its target
                    MovementManager.instance.LaunchAttackLocal(unit, Target, MovementManager.AttackModes.change, false);
                }
            }

            //if the unit didn't reach its destination yet but it's not really moving
            if (unit.MovementComp.DestinationReached == false && unit.MovementComp.IsMoving() == false)
                MovementManager.instance.LaunchAttackLocal(unit, Target, MovementManager.AttackModes.change, false); //make the unit move towards the target

            //if the unit is not in los or it can't attack while moving or it can but it's not in the target's range yet
            if (IsInLineOfSight() == false || (moveOnAttack == false && unit.MovementComp.IsMoving() == true) || IsTargetInRange() == false)
                return;

            //if the reload timer is done and we can play the attack animation and if the delay conditions are met
            if (reloadTimer <= 0.0f && canTriggerAnimation && (triggerAnimationInDelay || (delayTimer <= 0.0 && triggered)))
                TriggerAnimation();

            base.OnTargetUpdate();
        }

        //a method that triggers the unit's attack animation
        public void TriggerAnimation()
        {
            unit.SetAnimState(UnitAnimState.attacking);

            canTriggerAnimation = false; //can only play attack animation again after the attack is done
        }

        //a method called when an attack is complete:
        public override void OnAttackComplete()
        {
            base.OnAttackComplete();
            canTriggerAnimation = true; //attack animation can be triggered for the next attack
        }

        public override void SetTarget(FactionEntity newTarget)
        {
            MovementManager.instance.LaunchAttack(unit, newTarget, MovementManager.AttackModes.none, false);
        }

        //set the attack target locally
        public override void SetTargetLocal(FactionEntity newTarget)
        {
            unit.MovementComp.DestinationReached = false;
            base.SetTargetLocal(newTarget);
        }
    }
}