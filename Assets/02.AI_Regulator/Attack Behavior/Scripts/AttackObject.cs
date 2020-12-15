using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Attack Object script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(EffectObj))]
    public class AttackObject : MonoBehaviour
    {
        private int sourceFactionID; //the faction ID of the attacker that launched this attack object

        public bool isActive { private set; get; }

        //movement related attributes:
        public enum MvtTypes { linear, parabolic };
        [SerializeField]
        private MvtTypes mvtType = MvtTypes.linear;

        private bool mvtAttributesSet = false;

        //parabolic movement only:
        private Vector3 nextPos; //holds the next position to be taken by the attack object in the next frame
        private Vector3 middlePosition; //holds the next position to be taken by the attack object (when ignoring the height change).
        private Vector3 targetPosition; //the target position towards which the attack object is moving
        private float totalDistance; //the total distance that needs to be taken by the attack object to reach its target
        [SerializeField]
        private float maxHeight = 1.0f; //the maximum height that the attack object can reach in a parabolic movement
        private float startTime = 0.0f; //timer for the parabolic movement
        [SerializeField]
        private float minDistance = 2.0f; //minimum distance required to be able to have a parabolic movement
        private bool parabolicMvtEnabled = false; //is the distance greater than the min distance? if so, enabled the parabolic mvt

        private Vector3 mvtDirection;
        [SerializeField]
        private float speed = 10.0f;

        //damage related attributes:
        [SerializeField]
        private bool damageOnce = true;
        private bool didDamage = false;
        [SerializeField]
        private bool destroyOnDamage = true;
        private bool damageInDelay = false;
        [SerializeField]
        private bool childOnDamage = false; //when enabled, the attack object becomes a child object of its target when it deals damage to it

        [SerializeField]
        private LayerMask obstacleLayerMask = 0; //if the attack object collides with an object that has a layer inside this layer mask then it will stop moving and get destroyed.

        //delay related attributes:
        private float delayTimer;

        //effect related attributes:
        [SerializeField]
        private EffectObj triggerEffect = null; //triggered when the attack object is spawned (can be used for muzzle flash effect)
        [SerializeField]
        private EffectObj hitEffect = null; //triggered when the attack object damages a target faction entity.
        [SerializeField]
        private AudioClip hitAudio = null; //audio clip played when the attack object hits its target

        private AttackDamage damage; //handles damage settings and dealing damage.

        private EffectObj effectObjComp; //the effect obj component attached to the same gameobject

        private void Awake()
        {
            effectObjComp = GetComponent<EffectObj>();
        }

        private void Start()
        {
            speed *= GameManager.Instance.GetSpeedModifier(); //apply the speed modifier
        }

        public void Init(AttackEntity source, Vector3 targetPosition, float delayTimer, bool damageInDelay, Transform parent)
        {
            didDamage = false;

            sourceFactionID = source.FactionEntity.FactionID;

            damage = source.GetDamageSettings(); //get the damage settings from the attack source

            this.delayTimer = delayTimer;
            this.damageInDelay = damageInDelay;

            if (this.delayTimer > 0.0f) //only if there's delay time, we'll have the attack object as child of another object
                transform.SetParent(parent, true);

            Vector3 lookAt = targetPosition - transform.position; //where the attack object will be looking at.
            if (lookAt != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookAt);

            mvtAttributesSet = false; //movement attributes not yet set since there might be a delay in which the attack object might move
            this.targetPosition = targetPosition; //save target position.

            isActive = true;

        }

        void Update()
        {
            if (isActive == false || effectObjComp.CurrentState != EffectObj.State.running) //if the attack object is inactive, do not move it
                return;

            //if there's a delay do not move the attack object
            if (delayTimer > 0.0f)
            {
                delayTimer -= Time.deltaTime; //delay timer
                if (delayTimer <= 0.0f) //when done
                    transform.SetParent(null, true); //free attack object from parent
                return; //so that the attack object doesn't move while in delay
            }

            //if the movement attributes were not set, set them now
            if(mvtAttributesSet == false)
            {
                mvtAttributesSet = true;

                parabolicMvtEnabled = Vector3.Distance(transform.position, targetPosition) >= minDistance;
                mvtDirection = (targetPosition - transform.position).normalized;

                if (mvtType == MvtTypes.parabolic && parabolicMvtEnabled == true) //if this is a parabolic movement
                {
                    nextPos = transform.position; //reset next position
                    startTime = Time.time; //reset the parabolic mvt timer
                    totalDistance = Vector3.Distance(targetPosition, transform.position);
                    middlePosition = transform.position;
                }
                else //linear movement
                    transform.rotation = Quaternion.LookRotation(mvtDirection);
            }

            if (mvtType == MvtTypes.linear || parabolicMvtEnabled == false)
                transform.position += mvtDirection * speed * Time.deltaTime;
            else
            {
                //move the attack object on a parabola:
                transform.position = nextPos;

                //parabolicTimer += Time.deltaTime;

                System.Func<float, float> f = x => 4 * maxHeight * x * (-x + 1);
                middlePosition += mvtDirection * speed * Time.deltaTime;
                float currDistance = ((Time.time - startTime) * speed) / totalDistance;
                nextPos = new Vector3(middlePosition.x, f(currDistance) + middlePosition.y, middlePosition.z);

                Vector3 lookAt = nextPos - transform.position; //where the attack object will be looking at.
                if(lookAt != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookAt);
            }
        }

        //show an effect object as soon as the attack object is triggered
        public void ShowTriggerEffect()
        {
            EffectObjPool.SpawnEffectObj(triggerEffect, transform.position, Quaternion.identity); //Get the spawn effect obj
        }

        void OnTriggerEnter(Collider other)
        {
            //delay timer is still going and we can't damage in delay time + make sure that no damage has been applied or damaging multiple times is enabled
            if (isActive == false || effectObjComp.CurrentState != EffectObj.State.running || delayTimer > 0.0f && damageInDelay == false && (didDamage == false || damageOnce == false))
                return;

            if(RTSHelper.IsInLayerMask(obstacleLayerMask, other.gameObject.layer)) //Check if this is an obstacle that stops the attack object.
            {
                //show damage effect:
                EffectObjPool.SpawnEffectObj(hitEffect, transform.position, Quaternion.identity, other.transform); //Get the spawn effect obj
                AudioManager.PlayAudio(other.gameObject, hitAudio, false);

                effectObjComp.Disable(); //disable from the effect object component
                isActive = false; //set as inactive
                return;
            }

            SelectionEntity hitSelection = other.gameObject.GetComponent<SelectionEntity>();
            //if the attack object didn't enter in collision with a selection entity or it did but it was one belonging to resource
            if (hitSelection == null || hitSelection.FactionEntity == null)
                return;

            //make sure the faction entity belongs to an enemy faction and that it's not dead yet:
            if (hitSelection.FactionEntity.FactionID != sourceFactionID && hitSelection.FactionEntity.EntityHealthComp.IsDead() == false)
            {
                damage.TriggerAttack(hitSelection.FactionEntity, sourceFactionID, true); //deal damage
                didDamage = true;

                //show damage effect:
                EffectObjPool.SpawnEffectObj(hitEffect, transform.position, Quaternion.identity, hitSelection.FactionEntity.transform); //Get the spawn effect obj
                AudioManager.PlayAudio(hitSelection.FactionEntity.gameObject, hitAudio, false);

                if (childOnDamage == true) //if the attack object is supposed to become a child object of its target on damage:
                {
                    transform.SetParent(hitSelection.FactionEntity.transform, true);
                }

                if (destroyOnDamage == true) //if this object gets hidden on damage
                {
                    effectObjComp.Disable(); //disable from the effect object component
                    isActive = false; //set as inactive
                }
            }
        }
    }
}
