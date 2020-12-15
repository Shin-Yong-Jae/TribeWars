using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public enum UnitAnimState { idle, building, collecting, moving, attacking, healing, converting, takingDamage, dead } //all the possible animations states

    public class Unit : FactionEntity
    {
        [SerializeField]
        private int populationSlots = 1; //how many population slots will this unit occupy?
        public int GetPopulationSlots() { return populationSlots; }

        [SerializeField]
        private bool canBeConverted = true; //can this unit be converted?
        public bool CanBeConverted () { return canBeConverted; }

        public Building Creator { set; get; } //the building that produced this unit

        public int LastWorkerPosID { set; get;} //if this unit was constructing/collecting resource, this would the last worker position it had.

        //double clicking on the unit allows to select all units of the same type within a certain range
        private float doubleClickTimer;
        private bool clickedOnce = false;

        [SerializeField]
        private Animator animator = null; //the animator component
        private UnitAnimState currAnimatorState; //holds the current animator state
        public UnitAnimState GetCurrAnimatorState() { return currAnimatorState; }
        [SerializeField]
        private AnimatorOverrideController animatorOverrideController = null; //the unit's main animator override controller component
        public bool LockAnimState { set; get; }//When true, it won't be possible to change the animator state using the SetAnimState method.

        //Unit components:
        public UnitHealth HealthComp { private set; get; }
        public Converter ConverterComp { private set; get; }
        public UnitMovement MovementComp { private set; get; }
        public Wander WanderComp { private set; get; }
        public EscapeOnAttack EscapeComp { private set; get; }
        public Builder BuilderComp { private set; get; }
        public ResourceCollector CollectorComp { private set; get; }
        public Healer HealerComp { private set; get; }
        public UnitAttack AttackComp { set; get; }

        public override void Awake()
        {
            base.Awake();

            Type = FactionEntityTypes.unit;

            //get the unit's components
            HealthComp = GetComponent<UnitHealth>();
            ConverterComp = GetComponent<Converter>();
            MovementComp = GetComponent<UnitMovement>();
            WanderComp = GetComponent<Wander>();
            EscapeComp = GetComponent<EscapeOnAttack>();
            BuilderComp = GetComponent<Builder>();
            CollectorComp = GetComponent<ResourceCollector>();
            HealerComp = GetComponent<Healer>();
            AttackComp = GetComponent<UnitAttack>();
        }

        public override void UpdateAttackComp(AttackEntity attackEntity) { AttackComp = (UnitAttack)attackEntity; }

        public override void Start()
        {
            if (animator == null) //no animator component?
                Debug.LogError("[Unit] The " + GetName() + "'s Animator hasn't been assigned to the 'animator' field");

            if (animator != null) //as long as there's an animator component
            {
                if (animatorOverrideController == null) //if there's no animator override controller assigned..
                    animatorOverrideController = UnitManager.Instance.DefaultAnimController;
                ResetAnimatorOverrideController(); //set the default override controller
                //Set the initial animator state to idle
                SetAnimState(UnitAnimState.idle);
            }

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null) //no rigidbody component?
                Debug.LogError("[Unit] The " + GetName() + "'s main object is missing a rigidbody component");

            //rigidbody settings:
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;

            //initial settings for the double click
            clickedOnce = false;
            doubleClickTimer = 0.0f;

            Init(factionID);
        }

        public override void Init(int fID)
        {
            base.Init(fID);

            if (free == false) //if the unit belongs to an actual faction
            {
                FactionMgr.AddUnitToLists(this); //add the new unit to the unit's lists in the faction manager

                //if this is the local player's unit or we're in a single player game and the unit has been created by a valid building
                if ((GameManager.Instance.IsLocalPlayer(gameObject) || GameManager.MultiplayerGame == false) && Creator != null)
                    Creator.SendUnitToRallyPoint(this); //send unit to rally point
            }
            else //if this is a free unit
                foreach (GameManager.FactionInfo fi in GameManager.Instance.Factions) //add the unit in the enemy list of all other faction managers:
                    fi.FactionMgr.EnemyUnits.Add(this);

            MinimapIconManager.instance.AssignIcon(selection); //ask the minimap icon manager to create the a minimap icon for this unit

            CustomEvents.instance.OnUnitCreated(this); //trigger custom event
        }

        public override void Update()
        {
            base.Update();

            //double click timer:
            if (clickedOnce == true)
            {
                if (doubleClickTimer > 0)
                    doubleClickTimer -= Time.deltaTime;
                if (doubleClickTimer <= 0)
                    clickedOnce = false;
            }
        }

        //a method that is called when a mouse click on this portal is detected
        public void OnMouseClick()
        {
            if (clickedOnce == false)
            { //if the player hasn't clicked on this portal shortly before this click
                DisableSelectionFlash(); //disable the selection flash

                SelectionManager.instance.SelectUnit(this, SelectionManager.instance.MultipleSelectionKeyDown); //select the unit

                if (SelectionManager.instance.MultipleSelectionKeyDown == false) //if the player doesn't have the multiple selection key down (not looking to select multiple units one by one)
                { 
                    //start the double click timer
                    doubleClickTimer = 0.5f;
                    clickedOnce = true;
                }
            }
            else //if this is the second click (double click), select all units of the same type within a certain range
                SelectionManager.instance.SelectUnitsInRange(this);
        }

        //a method that converts this unit to the converter's faction
        public void Convert(Unit converter)
        {
            if (converter.FactionID == FactionID) //if the converter and this unit have the same faction, then, what a waste of time and resources.
                return;

            if (GameManager.MultiplayerGame == false) //if this is a single player game
                ConvertLocal(converter); //convert unit directly
            else if (GameManager.Instance.IsLocalPlayer(gameObject)) //online game and this is the local player
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.convert,
                    initialPosition = transform.position
                };

                InputManager.SendInput(newInput, gameObject, converter.gameObject); //send conversion input to the input manager
            }
        }

        //a method that converts this unit to the converter's faction, locally
        public void ConvertLocal(Unit converter)
        {
            RemoveFromFaction(false); //remove it first from its current faction

            free = false; //if the unit was free before, it's no longer free
            AssignFaction(converter.FactionMgr); //assign the new faction

            if (TaskLauncherComp != null) //if the unit has a task launcher 
                TaskLauncherComp.SetFactionInfo(); //update the task launcher info

            converter.ConverterComp.EnableConvertEffect(); //enable the conversion effect on the converter

            CustomEvents.instance.OnUnitConverted(converter, this); //trigger the custom event

            MovementComp.Stop(); //stop the unit's movement

            CancelJob(jobType.all); //cancel all jobs
        }

        public enum jobType { attack, building, collecting, healing, converting, all} //these are the components that the unit is allowed to have.

        //this method allows to cancel one or more jobs.
        public void CancelJob (jobType[] jobs)
        {
            foreach (jobType job in jobs)
                CancelJob(job);
        }

        public void CancelJob (jobType job)
        {
            if (AttackComp && (job == jobType.all || job == jobType.attack))
                AttackComp.Stop();
            if (BuilderComp && (job == jobType.all || job == jobType.building))
                BuilderComp.Stop();
            if (CollectorComp && (job == jobType.all || job == jobType.collecting))
            {
                CollectorComp.CancelDropOff();
                CollectorComp.Stop();
            }
            if (HealerComp && (job == jobType.all || job == jobType.healing))
                HealerComp.Stop();
            if (ConverterComp && (job == jobType.all || job == jobType.converting))
                ConverterComp.Stop();
        }

        //a method that assings a new faction for the unit
        public void AssignFaction(FactionManager factionMgr)
        {
            FactionMgr = factionMgr; //set the new faction
            FactionID = FactionMgr.FactionID; //set the new faction ID

            Creator = GameManager.Instance.Factions[FactionID].CapitalBuilding; //make the unit's producer, the capital of the new faction
            FactionMgr.AddUnitToLists(this); //add the unit to the new faction's lists
            SetFactionColors(); //set the new faction colors

            selection.UpdateMinimapIconColor(); //assign the new faction color for the unit in the minimap icon
        }

        //a method that removes this unit from its current faction
        public void RemoveFromFaction(bool destroyed)
        {
            if (free == true) //if this doesn't belong to any faction
            {
                foreach (GameManager.FactionInfo fi in GameManager.Instance.Factions)
                    fi.FactionMgr.EnemyUnits.Remove(this); //remove it from the enemies of all current factions
                return;
            }

            //unit belongs to a faction:
            FactionMgr.RemoveUnitFromLists(this); //Remove from current faction lists

            if (TaskLauncherComp != null)  //if the unit has a task manager and there are pending tasks there
                TaskLauncherComp.CancelAllInProgressTasks(); //cancel all the in progress tasks

            if (APCComp != null) //if this unit has an APC component attached to it
                APCComp.EjectAll(destroyed); //remove all the units stored in the APC

            GameManager.Instance.Factions[FactionID].UpdateCurrentPopulation(-GetPopulationSlots());
        }

        //See if the unit is in idle state or not:
        public bool IsIdle()
        {
            return !(MovementComp.IsMoving()
                || (BuilderComp && BuilderComp.IsActive())
                || (CollectorComp && CollectorComp.IsActive())
                || (AttackComp && AttackComp.IsActive() && AttackComp.Target != null)
                || (HealerComp && HealerComp.IsActive())
                || (ConverterComp && ConverterComp.IsActive()));
        }

        //a method to change the animator state
        public void SetAnimState(UnitAnimState newState)
        {
            if (LockAnimState == true || animator == null) //if our animation state is locked or there's no animator assigned then don't proceed.
                return;

            currAnimatorState = newState; //update the current animator state

            animator.SetBool("IsIdle", currAnimatorState==UnitAnimState.idle);
            animator.SetBool("IsBuilding", currAnimatorState == UnitAnimState.building);
            animator.SetBool("IsCollecting", currAnimatorState == UnitAnimState.collecting);
            animator.SetBool("IsMoving", currAnimatorState == UnitAnimState.moving);
            animator.SetBool("IsAttacking", currAnimatorState == UnitAnimState.attacking);
            animator.SetBool("IsHealing", currAnimatorState == UnitAnimState.healing);
            animator.SetBool("IsConverting", currAnimatorState == UnitAnimState.converting);
            if (currAnimatorState == UnitAnimState.takingDamage && HealthComp.IsDamageAnimationEnabled() == true) //if taking damage animation is enabled:
                animator.SetBool("TookDamage", true);
            else
                animator.SetBool("TookDamage", false);
            animator.SetBool("IsDead", currAnimatorState == UnitAnimState.dead);
        }

        //a method to change the animator override controller:
        public void SetAnimatorOverrideController(AnimatorOverrideController newOverrideController)
        {
            if (newOverrideController == null)
                return;

            animator.runtimeAnimatorController = newOverrideController; //set the runtime controller to the new override controller
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f); //reload the runtime animator controller
        }

        //a method that changes the animator override controller back to the default one
        public void ResetAnimatorOverrideController ()
        {
            SetAnimatorOverrideController(animatorOverrideController);
        }

        //get the radius of the unit in the movement component
        public override float GetRadius () { return MovementComp.GetAgentRadius(); }
    }
}
