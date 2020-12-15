using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Unit Component script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[System.Serializable]
public class AutoUnitBehavior
{
    [SerializeField]
    private bool enabled = false; //if set to true, when the unit is idle, it will search for a target and automatically move to active state
    public bool IsEnabled() { return enabled; }
    public void Toggle(bool enable) { enabled = enable; }

    [SerializeField]
    private float searchReload = 5.0f; //time needed before the unit starts another search
    float searchTimer;

    [SerializeField]
    private float searchRange = 20.0f; //the range where the unit where search for a target
    public float GetSearchRange() { return searchRange; }
    public void ReloadSearchTimer() { searchTimer = searchReload; }

    //a method that updates the search timer
    public void UpdateTimer()
    {
        if (searchTimer >= 0)
            searchTimer -= Time.deltaTime;
    }

    //can the unit search for a target?
    public bool CanSearch()
    {
        if (searchTimer <= 0.0f) //if the search timer is through
        {
            ReloadSearchTimer(); //reload the timer
            return true; //allow unit to search for target
        }
        return false;
    }
}

namespace RTSEngine
{
    public abstract class UnitComponent<E> : MonoBehaviour where E : MonoBehaviour
    {
        protected Unit unit; //the unit's main component

        protected bool inProgress = false; //is the unit currently performing what this unit component is supposed to do?
        public bool IsInProgress() { return inProgress; }

        protected E target; //the target object that this unit component deals with.
        public bool IsActive() { return target != null; } //if the unit has a target then this component is active
        public E GetTarget() { return target; }

        protected float timer;

        [SerializeField]
        protected GameObject inProgressObject; //a gameobject (child of the main unit object) that is activated when the unit's job is in progress

        [SerializeField]
        public EffectObj sourceEffect = null; //triggered on the source unit when this component is in progress.
        private EffectObj currSourceEffect;
        [SerializeField]
        public EffectObj targetEffect = null; //triggered on the unit's target when this component is in progress.
        private EffectObj currTargetEffect;

        [SerializeField]
        protected AudioClip orderAudio; //audio clip played when the unit is ordered to perform the task associated with this component
        public AudioClip GetOrderAudio() { return orderAudio; }

        [SerializeField]
        protected AutoUnitBehavior autoBehavior = new AutoUnitBehavior(); //can the unit search for a target automatically?

        public virtual void Awake()
        {
            unit = GetComponent<Unit>();
            autoBehavior.ReloadSearchTimer(); //reload the search timer of the auto behavior
        }

        public virtual void Start()
        {
            if (unit.FactionID != GameManager.PlayerFactionID) //if this unit doesn't belong to the player's local faction
                autoBehavior.Toggle(false); //disable the automatic behavior.
        }

        public virtual void Update()
        {
            if (unit.HealthComp.IsDead() == true) //if the unit is dead, do not proceed.
                return;

            if (target != null) //unit has target -> active
                OnActiveUpdate(0.0f, UnitAnimState.idle, null); //on active update
            else //no target? -> inactive
                OnInactiveUpdate();
        }

        //update in case the unit has a target
        protected virtual bool OnActiveUpdate(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (breakCondition) //if break condition is met then the unit can no longer be active
            {
                unit.MovementComp.Stop(); //stop the movement of the unit in case it was moving towards its target
                Stop(); //cancel the current job
                return false;
            }

            if (unit.MovementComp.DestinationReached && inProgress == false && inProgressEnableCondition) //if the unit has reached its target and it hasn't started its job yet + the provided additional condition
                OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            if (inProgress == true && inProgressCondition) //if the unit's job is currently in progress
            {
                if (timer > 0) //construction timer
                    timer -= Time.deltaTime;
                else
                {
                    OnInProgress();
                    timer = reloadTime; //reload timer
                }
            }

            return true;
        }

        //called when the unit's job is enabled
        protected virtual void OnInProgressEnabled(float reloadTime, UnitAnimState activeAnimState, AudioClip[] inProgressAudio)
        {
            unit.SetAnimState(activeAnimState);
            if (inProgressAudio.Length > 0) //if the unit has at least one in progress audio clip
                AudioManager.PlayAudio(gameObject, inProgressAudio[Random.Range(0, inProgressAudio.Length - 1)], true); //play a random one

            if (inProgressObject != null) //show the in progress object
                inProgressObject.SetActive(true);

            timer = reloadTime; //start timer
            inProgress = true; //the unit's job is now in progress

            if(sourceEffect != null)
                currSourceEffect = EffectObjPool.SpawnEffectObj(
                    sourceEffect,
                    transform.position,
                    sourceEffect.transform.rotation,
                    transform,
                    false); //spawn the source effect on the source unit and don't enable the life timer

            if (targetEffect != null)
                currTargetEffect = EffectObjPool.SpawnEffectObj(
                    targetEffect,
                    target.transform.position,
                    targetEffect.transform.rotation,
                    target.transform,
                    false); //spawn the target effect on the target and don't enable the life timer
        }

        //method called when the unit's progresses in its active job
        protected virtual void OnInProgress()
        {

        }

        //update when the unit doesn't have a target
        protected virtual void OnInactiveUpdate()
        {
            if (inProgress == true) //if the unit doesn't have a target but its job is marked as in progress
                Stop(); //cancel job

            if ((GameManager.MultiplayerGame == false || GameManager.Instance.IsLocalPlayer(gameObject)) && autoBehavior.IsEnabled() == true && unit.IsIdle() == true) //if the auto behavior is, the unit is idle and this is the local player's faction or a single player game
            {
                if (autoBehavior.CanSearch() == true) //can the unit search for a target
                {
                    OnTargetSearch();
                }
                else
                    autoBehavior.UpdateTimer(); //update the timer.
            }
        }

        //called when the unit's auto behavior launches a search for a target
        protected virtual void OnTargetSearch()
        {

        }

        //called when the unit attempts to set a new target
        public virtual void SetTarget(E newTarget, InputMode targetMode = InputMode.none)
        {
            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                SetTargetLocal(newTarget);
            else if (GameManager.Instance.IsLocalPlayer(gameObject)) //multiplayer game and this is the unit's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)targetMode,
                    initialPosition = transform.position,
                    targetPosition = newTarget.transform.position
                };

                InputManager.SendInput(newInput, gameObject, newTarget.gameObject);
            }
        }

        public abstract void SetTargetLocal(E newTarget);

        public virtual bool Stop()
        {
            if (IsActive() == false) //the component is not active
                return false; //do not proceed

            if (inProgressObject != null) //hide the in progress object
                inProgressObject.SetActive(false);

            //reset construction settings
            target = null;
            inProgress = false;

            unit.SetAnimState(UnitAnimState.idle); //back to idle state

            AudioManager.StopAudio(gameObject); //stop construction audio

            if (currSourceEffect != null) //if the source unit effect was assigned and it's still valid
            {
                currSourceEffect.Disable(); //stop it
                currSourceEffect = null;
            }

            if (currTargetEffect != null) //if a target effect was assigned and it's still valid
            {
                currTargetEffect.Disable(); //stop it
                currTargetEffect = null;
            }

            return true;
        }
    }
}
