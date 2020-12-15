using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    //this requires a Faction Entity component
    [RequireComponent(typeof(FactionEntity))]
    public abstract class FactionEntityHealth : MonoBehaviour
    {
        private FactionEntity factionEntity; //the faction's entity component

        //health settings:

        //maximum health points of the faction entity
        [SerializeField]
        private int maxHealth = 100;
        //the maximum health must always be > 0.0
        public int MaxHealth
        {
            set
            {
                if (value > 0.0)
                    maxHealth = value;
            }
            get
            {
                return maxHealth;
            }
        }

        //current health points of the faction entity
        public int CurrHealth { set; get; }

        [SerializeField]
        private float hoverHealthBarY = 4.0f; //this is the height of the hover health bar (in case the feature is enabled).
        public float GetHoverHealthBarY () { return hoverHealthBarY; }
        protected bool isDead = false; //is this faction entity dead? 
        public bool IsDead () { return isDead; }

        //Damage settings:
        [SerializeField]
        private bool canBeAttacked = true; //can this faction entity be attacked?
        public bool CanBeAttacked () { return canBeAttacked; }
        [SerializeField]
        private bool takeDamage = true; //does this faction entity lose damage when it is attacked?
        public bool CanTakeDamage () { return takeDamage; }
        [SerializeField]
        private EffectObj damageEffect = null; //appears when a damage is received in the contact point between the attack object and this faction entity
        public EffectObj GetDamageEffect() { return damageEffect; }

        //Destruction settings:
        public bool IsDestroyed { set; get; } //is the faction entity destroyed?
        protected int killerFactionID = -1; //the faction ID of the killer/destroyer of this faction entity
        [SerializeField]
        private bool destroyObject = true; //destroy the object on destruction?
        [SerializeField]
        private float destroyObjectTime = 0.0f; //if the above bool is set to true, this is the time it will take to destroy the object.
        [SerializeField]
        private ResourceManager.Resources[] destroyAward = new ResourceManager.Resources[0]; //these resources will be awarded to the faction that destroyed this faction entity

        //Destruction effects:
        [SerializeField]
        private AudioClip destructionAudio = null; //audio played when the faction entity is destroyed.
        [SerializeField]
        private EffectObj destructionEffect = null; //this effect will be shown on the faction entity's position when it is destroyed.

        //other components:
        public List<DamageOverTime> DotComps { set; get; }

        public virtual void Awake()
        {
            factionEntity = gameObject.GetComponent<FactionEntity>(); //get the Faction Entity component.
            //initial settings:
            isDead = false; //faction entity is not dead by default.

            DotComps = new List<DamageOverTime>();
        }

        //add/remove health to the faction entity's:
        public void AddHealth(int value, FactionEntity source)
        {
            if (GameManager.MultiplayerGame == false) //if it's a single player game
            {
                AddHealthLocal(value, source); //add the health directly
            }
            else //multiplayer game
            {
                if (GameManager.Instance.IsLocalPlayer(gameObject)) //make sure that it's this faction entity belongs to the local player
                {
                    //crete new input
                    NetworkInput newInput = new NetworkInput
                    {
                        sourceMode = (byte)InputMode.factionEntity,
                        targetMode = (byte)InputMode.health,
                        value = value
                    };
                    //send the input to the input manager
                    InputManager.SendInput(newInput, gameObject, (source != null) ? source.gameObject : null);
                }
            }
        }

        //add health to the faction entity locally
        public void AddHealthLocal(int value, FactionEntity source)
        {
            //if the faction entity doesn't take damage and the health points to add is negative (damage):
            if (takeDamage == false && value < 0.0f)
                return; //don't proceed.

            CurrHealth += value; //add the input value to the current health value
            if (CurrHealth >= MaxHealth) //if the current health is above the maximum allowed health
                OnMaxHealthReached(value, source);

            //if the mouse is over this unit
            if (UIManager.instance.IsHoverSource(factionEntity.GetSelection()))
                UIManager.instance.UpdateHoverHealthBar(CurrHealth, MaxHealth); //update hover health bar

            //Update health UI:
            //Checking if the entity that has just got its health updated is currently selected:
            if (SelectionManager.instance.IsFactionEntitySelected(factionEntity))
                UIManager.instance.UpdateFactionEntityHealthUI(factionEntity); //update the faction entity health

            if (CurrHealth <= 0.0f && isDead == false)
                OnZeroHealth(value, source);
            //the faction entity isn't "dead"
            else
                OnHealthUpdated(value, source);
        }

        //a method called when the faction entity reaches max health:
        public virtual void OnMaxHealthReached (int value, FactionEntity source)
        {
            CurrHealth = MaxHealth;
        }

        //a method called when the faction entity's health hits null:
        public virtual void OnZeroHealth (int value, FactionEntity source)
        {
            //set it back to 0.0f as we don't allow negative health values.
            CurrHealth = 0;

            //if the faction entity isn't already dead:
            if (isDead == false)
            {
                isDead = true; //mark as dead
                               //is there a valid source that caused the death of this faction entity?
                if (source != null)
                    //award the destroy award to the source if the source is not the same faction ID:
                    if (destroyAward.Length > 0 && source.FactionID != factionEntity.FactionID)
                        for (int i = 0; i < destroyAward.Length; i++)
                            //award destroy resources to source:
                            ResourceManager.instance.AddResource(source.FactionID, destroyAward[i].Name, destroyAward[i].Amount);


                //destroy the faction entity
                DestroyFactionEntity(false);
            }
        }

        //a method called when the faction entity's health has been updated:
        public virtual void OnHealthUpdated (int value, FactionEntity source)
        {
            if (value < 0.0)
            {
                //if this is the local player's faction ID and the attack warning manager is active:
                if (factionEntity.FactionID == GameManager.PlayerFactionID && AttackWarningManager.instance != null)
                    AttackWarningManager.instance.AddAttackWarning(this.gameObject); //show attack warning on minimap
            }
        }

        //a method called to destroy the faction entity:
        public void DestroyFactionEntity (bool upgrade)
        {
            if (GameManager.MultiplayerGame == false) //if it's a single player game
                DestroyFactionEntityLocal(upgrade); //destroy faction entity directly
            else //multiplayer game
            {
                if (GameManager.Instance.IsLocalPlayer(gameObject)) //make sure that it's this faction entity belongs to the local player
                {
                    //send input action to the input manager
                    NetworkInput NewInputAction = new NetworkInput
                    {
                        sourceMode = (byte)InputMode.destroy,
                        targetMode = (byte)InputMode.factionEntity,
                        value = (upgrade == true) ? 1 : 0 //when upgrade == true, then set to 1. if not set to 0
                    };
                    InputManager.SendInput(NewInputAction, gameObject, null); //send to input manager
                }
            }
        }
        
        //a method that destroys a faction entity locally
        public virtual void DestroyFactionEntityLocal(bool upgrade)
        {
            //faction entity death:
            isDead = true;
            CurrHealth = 0;

            //if the unit has a task launcher:
            if (factionEntity.TaskLauncherComp != null)
            {
                //Launch the delegate event:
                CustomEvents.instance.OnTaskLauncherRemoved(factionEntity.TaskLauncherComp);

                //If there are pending tasks, stop them and give the faction back the resources of these tasks:
                factionEntity.TaskLauncherComp.CancelAllInProgressTasks();
            }

            if (factionEntity.IsFree() == false && factionEntity.APCComp != null) //if this is not a free faction entity and it has a APC component
                factionEntity.APCComp.EjectAll(true);

            if (UIManager.instance.IsHoverSource(factionEntity.GetSelection())) //if the mouse is over this faction entity
                UIManager.instance.TriggerHoverHealthBar(false, factionEntity.GetSelection(), 0.0f); //stop displaying the hover health bar

            //remove the minimap icon:
            factionEntity.GetSelection().DisableMinimapIcon();

            //Spawn the destruction effect obj if it exists:
            if (upgrade == false && destructionEffect != null)
            {
                //get the destruction effect from the pool
                EffectObj newDestructionEffect = EffectObjPool.SpawnEffectObj(destructionEffect, transform.position, Quaternion.identity);

                //destruction sound effect
                if (destructionAudio != null)
                {
                    //Check if the destruction effect object has an audio source:
                    if (newDestructionEffect.GetComponent<AudioSource>() != null)
                        AudioManager.PlayAudio(newDestructionEffect.gameObject, destructionAudio, false); //play the destruction audio
                    else
                        Debug.LogError("A destruction audio clip has been assigned but the destruction effect object doesn't have an audio source!");
                }
            }

            //Destroy the faction entity's object:
            if (destroyObject == true) //only if object destruction is allowed
            {
                Destroy(gameObject, destroyObjectTime);
                IsDestroyed = true;
            }
        }
    }
}
