using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

/* Effect Object Pooling script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class EffectObjPool : MonoBehaviour
    {
        public static EffectObjPool instance { private set; get; }

        //because instantiating and destroying objects is heavy on memory and since we need to show/hide effect objects multiple times in a game...
        //...this component will handle pooling those effect objects.

        private Dictionary<string, Queue<EffectObj>> effectObjs = new Dictionary<string, Queue<EffectObj>>(); //this holds all inactive effect objects of different types.

        void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        //this method searches for a hidden effect object with a certain code so that it can be used again.
        public EffectObj GetFreeEffectObj(EffectObj prefab)
        {
            Assert.IsTrue(prefab != null, "[Effect Object Pool] invalid effect object prefab.");

            Queue<EffectObj> currentQueue;
            if(effectObjs.TryGetValue(prefab.GetCode(), out currentQueue) == false) //if the queue for this effect object type is not found
            {
                currentQueue = new Queue<EffectObj>();
                effectObjs.Add(prefab.GetCode(), currentQueue); //add it
            }

            if (currentQueue.Count == 0) //if the queue is empty then we need to create a new effect object of this types
            {
                currentQueue.Enqueue(Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<EffectObj>());
            }
            return currentQueue.Dequeue(); //return the first inactive effect object in this queue
        }

        public void AddFreeEffectObj (EffectObj instance)
        {
            Queue<EffectObj> currentQueue = new Queue<EffectObj>();
            if (effectObjs.TryGetValue(instance.GetCode(), out currentQueue) == false) //if the queue for this effect object type is not found
            {
                effectObjs.Add(instance.GetCode(), currentQueue); //add it
            }

            currentQueue.Enqueue(instance); //add the effect object to the right queue
        }

        //a method that spawns an effect object considering a couple of options
        public static EffectObj SpawnEffectObj(EffectObj prefab, Vector3 spawnPostion, Quaternion spawnRotation, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0.0f)
        {
            if (prefab == null)
                return null;

            //get the attack effect (either create it or get one tht is inactive):
            EffectObj newEffect = instance.GetFreeEffectObj(prefab);

            //set the effect's position and rotation
            newEffect.transform.position = spawnPostion;
            newEffect.transform.rotation = spawnRotation;
            
            //make the object child of the assigned parent transform
            newEffect.transform.SetParent(parent, true);

            newEffect.ReloadTimer(enableLifeTime, autoLifeTime, lifeTime); //reload lifetime

            newEffect.Enable();

            return newEffect; 
        }
    }
}