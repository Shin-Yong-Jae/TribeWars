using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [RequireComponent(typeof(Unit))]
    public class UnitHealth : FactionEntityHealth
    {
        private Unit unit; //the main unit's component

        [SerializeField]
        private bool stopMovingOnDamage = false; //stop the movement when taking damage? 
        [SerializeField]
        private bool enableDamageAnimation = false; //enable playing an animation when taking damage?
        public bool IsDamageAnimationActive () { return damageAnimationTimer > 0; }
        public bool IsDamageAnimationEnabled() { return enableDamageAnimation; }
        private bool damageAnimationActive = false;
        [SerializeField]
        private float damageAnimationDuration = 0.2f; //the duration of the animation of taking damage is manually defined here.
        float damageAnimationTimer;

        public override void Awake()
        {
            base.Awake();
            unit = GetComponent<Unit>();
            CurrHealth = MaxHealth; //unit has maximum health by default
        }

        //a method called when the unit's health has been updated:
        public override void OnHealthUpdated(int value, FactionEntity source)
        {
            base.OnHealthUpdated(value, source);

            if (value < 0) //if the unit's health has been decreased
            {
                unit.SetAnimState(UnitAnimState.takingDamage); //set the animator state to taking damage.

                if (stopMovingOnDamage == true) //stop player movement on damage if this is set to true
                    unit.MovementComp.Stop();
                if (enableDamageAnimation == true) //if the damage animation is enabled
                {
                    damageAnimationTimer = damageAnimationDuration; //start the timer.
                    damageAnimationActive = true; //mark as active
                }

                if (source != null && (GameManager.MultiplayerGame == false || GameManager.Instance.IsLocalPlayer(gameObject) == true)) //if the attack source is known and this is either a single player game or the local player's unit
                {
                    if (unit.AttackComp != null && unit.AttackComp.CanEngageWhenAttacked() == true && unit.IsIdle() == true) //if the unit has an attack component and it can attack back and it is idle
                        MovementManager.instance.LaunchAttack(unit, source, MovementManager.AttackModes.none, false); //launch attack at the source

                    TriggerEscapeOnAttack(); //attempt to trigger the escape on attack behavior if it is enabled
                }
            }
        }

        public override void DestroyFactionEntityLocal(bool upgrade)
        {
            base.DestroyFactionEntityLocal(upgrade);

            unit.RemoveFromFaction(true); //remove the unit from its faction

            SelectionManager.instance.DeselectUnit(unit); //deselect the unit if it was selected

            unit.MovementComp.DestroyTargetPositionCollider(); //destroy the target collider position
            unit.SetAnimState(UnitAnimState.dead); //mark as dead
            unit.MovementComp.Stop(); //stop the unit's movement

            unit.CancelJob(Unit.jobType.all); //cancel all units jobs

            CustomEvents.instance.OnUnitDead(unit); //trigger custom event
        }

        //a method that attempts to trigger the escape on attack:
        public void TriggerEscapeOnAttack ()
        {
            if (enableDamageAnimation == false && unit.EscapeComp != null) //if the damage animation is disabled and the escape on attack behavior is enabled 
                unit.EscapeComp.Trigger();
        }

        protected  void Update()
        {
            if (isDead == true || (GameManager.MultiplayerGame == true && GameManager.Instance.IsLocalPlayer(gameObject) == false)) //if this is a multiplayer game and this is not the local player or the unit is dead
                return; //do not proceed

            if (enableDamageAnimation == true && damageAnimationActive == true) //if the damage animation is enabled and it is currently active
            {
                if (damageAnimationTimer > 0) //the damage animation timer
                    damageAnimationTimer -= Time.deltaTime;
                if (damageAnimationTimer < 0)
                {
                    damageAnimationActive = false;
                    damageAnimationTimer = 0.0f; //reset the timer
                    TriggerEscapeOnAttack(); //attempt to trigger the escape on attack behavior if it is enabled
                }
            }
        }
    }
}
