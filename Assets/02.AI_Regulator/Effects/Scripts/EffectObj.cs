using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/* Effect Object script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class EffectObj : MonoBehaviour {

        [SerializeField]
        private string code = ""; //each effect object prefab must have a unique code
        public string GetCode () { return code; }

        [SerializeField]
        private bool enableLifeTime = true; //Control the lifetime of this effect object using the time right below?

        [SerializeField]
        private float defaultLifeTime = 3.0f; //The default duration during which the effect object will be shown before it's deactivated.
        private float timer;

        [SerializeField]
        private float disableTime = 0.0f; //when > 0, the disable events will be invoked and then timer with this length will start and then the effect object will be hidden

        public enum State {inactive, running, disabling};
        public State CurrentState { set; get; }

        //method that enables/disables the life timer and sets its duration
        public void ReloadTimer (bool enable, bool useDefault = true, float duration = 0.0f) {
            timer = useDefault ? defaultLifeTime : duration;
            enableLifeTime = enable;
        }

        [SerializeField]
        private Vector3 spawnPositionOffset = Vector3.zero;

        [SerializeField]
        private UnityEvent onEnableEvent = null; //invoked when the effect object is enabled.
        [SerializeField]
        private UnityEvent onDisableEvent = null; //invoked when the effect object is disabled.

        void Update ()
		{
            if (CurrentState != State.inactive) //if the effect object is not inactive then run timer
            {
                if (timer > 0.0f)
                    timer -= Time.deltaTime;
                else //life timer is through
                {
                    switch (CurrentState)
                    {
                        case State.running: //if the effect object is running (active)
                            if (enableLifeTime == true) //make sure the life time is enabled
                                Disable(); //disable the effect object
                            break;
                        case State.disabling: //if the effect object is getting disabled
                            DisableInternal(); //disable the effect object completely
                            break;

                    }
                }
            }
        }

        //enable the effect object
        public void Enable()
        {
            gameObject.SetActive(true);
            CurrentState = State.running;

            transform.position += spawnPositionOffset; //set spawn position offset.

            onEnableEvent.Invoke(); //invoke the event
        }

        //hide the effect object
        public void Disable()
        {
            if (CurrentState == State.disabling) //if the effect object is already being disabled
                return;

            onDisableEvent.Invoke(); //invoke the event.

            timer = disableTime; //start the disable timer
            CurrentState = State.disabling; //we're now disabling the effect object
        }

        private void DisableInternal ()
        {
            transform.SetParent(null, true); //Make sure it has no parent object anymore.
            CurrentState = State.inactive;
            gameObject.SetActive(false);
            EffectObjPool.instance.AddFreeEffectObj(this);
        }
    }
}