using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [RequireComponent(typeof(UnitMovement))]
    public class EscapeOnAttack : MonoBehaviour
    {
        private Unit unit; //the main unit's component

        [SerializeField]
        private bool isActive = true; //is this component active?
        public bool IsActive() { return isActive; }

        [SerializeField]
        private bool NPCOnly = false; //when enabled, then this component will only be active for NPC units

        [SerializeField]
        private FloatRange range = new FloatRange(20.0f, 40.0f); //the range where the unit will escape.

        [SerializeField]
        private float speed = 10.0f; //the unit's movement speed can be modified when escaping
        public float GetSpeed () { return speed; }

        [SerializeField]
        private AnimatorOverrideController escapeAnimatorOverride = null; //escape animator override that is activated when the unit is escaping
        //the above animator override controller allows the unit to have a different movement animation when running

        private void Awake ()
        {
            unit = GetComponent<Unit>();
        }

        private void Start()
        {
            if (NPCOnly == true && GameManager.Instance.Factions[unit.FactionID].IsNPCFaction() == false)
                isActive = false;

            speed *= GameManager.Instance.GetSpeedModifier(); //apply the speed modifier
        }

        //a method to trigger the escape on attack behavior
        public void Trigger()
        {
            if (isActive == false) //do not proceed if the component is not active
                return;

            Vector3 targetPosition = MovementManager.instance.GetRandomMovablePosition(transform.position, range.getRandomValue(), unit, unit.MovementComp.GetAgentAreaMask()); //find a random position to escape to

            if (GameManager.MultiplayerGame == false) //single player game
                TriggerLocal(targetPosition);
            else if (GameManager.Instance.IsLocalPlayer(gameObject) == true) //multiplayer game and this is the local player
            {
                NetworkInput newInput = new NetworkInput() //create new input for the escape task
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.unitEscape,
                    initialPosition = transform.position,
                    targetPosition = targetPosition
                };
                InputManager.SendInput(newInput, gameObject, null); //send input to input manager

            }
        }

        //a method to locally trigger the escape on attack behavior
        public void TriggerLocal(Vector3 targetPosition)
        {
            if (isActive == false) //do not proceed if the component is not active
                return;

            MovementManager.instance.PrepareMove(unit, targetPosition, 0.0f, null, InputMode.unitEscape, false); //move the unit locally

            if (escapeAnimatorOverride != null) //update the runtime animator controller to this one if it has been assigned
                unit.SetAnimatorOverrideController(escapeAnimatorOverride);
        }
    }
}
