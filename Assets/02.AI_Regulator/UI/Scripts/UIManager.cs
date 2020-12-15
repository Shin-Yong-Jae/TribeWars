using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

/* UI Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

//THIS COMPONENT IS SUBJECT TO REFACTORING/REWRITE IN FUTURE UPDATES.

namespace RTSEngine
{
	public class UIManager : MonoBehaviour 
	{
        public static UIManager instance; //we'll have the one instance of this component here so it's easier to access from other components.

		[Header("Menus:")]
		//Winng and loosing menus:
		public GameObject WinningMenu; //activated when the player wins the game.
		public GameObject LoosingMenu; //activated when the player loses the game.
        public GameObject PauseMenu; //activated when the player is pausing.
        public GameObject FreezeMenu; //activated when waiting for players to sync in a MP game.

        [Header("Tasks:")]
		//The buttons that get activated when a unit/building is selected. 
		public Button BaseTaskButton; 
		[HideInInspector]
		public List<Button> TaskButtons; //all the task buttons
		int ActiveTaskButtons = 0;
		public Transform TaskButtonsParent; //all the task buttons are children of this object.
		public bool UseTaskParentCategories;
		public Transform[] TaskParentCategories; //you can assign building tasks to different transform parents in order to organize them for the player.

        [Header("In Progress Tasks:")]
		//In Progress building task info:
		public Button FirstInProgressTaskButton; 
		public Image FirstInProgressTaskBarEmpty;
		public Image FirstInProgressTaskBarFull;
		public Button BaseInProgressTaskButton; 
		[HideInInspector]
		public List<Button> InProgressTaskButtons; //The buttons that show the in progress tasks of a building when selected.
		public Transform InProgressTaskButtonsParent; //all the in progress building tasks are children of this object.

		[Header("Selection Info:")]
		//Selection Info vars:
		public Transform SingleSelectionParent; //the selection menu
		public Text SelectionName;
		public Text SelectionDescription;
        public bool ShowPopulationSlots = true;
		public Text SelectionHealth;
		public Image SelectionIcon;
		public Image SelectionHealthBarEmpty;
		public Image SelectionHealthBarFull;

		[Header("Multiple Selection:")]
		//Multiple selection icons:
		public Image BaseMultipleSelectionIcon;
		[HideInInspector]
		public List<Image> MultipleSelectionIcons;
		public Transform MultipleSelectionParent; //all the selected icons are children of this object.

        [Header("Hover Health Bar:")]
        public bool EnableHoverHealthBar = true; //if set to true then whenever the mouse hovers over a unit/building, a health bar will appear on top of them
        public bool PlayerFactionOnly = true; //only show the hover health bar for friendly units/buildings or for all factions
        public Canvas HoverHealthBarCanvas; //the canvas that holds the health bar sprites
        public RectTransform HoverHealthBarEmpty; //when the hover health bar is empty
        public RectTransform HoverHealthBarFull; //when the hover health bar is full
        bool HoverHealthBarActive = false; //is the hover health bar active or not?
        SelectionEntity HoverSource; //the health bar will be showing this object's health when it's enabled.

        [Header("Tooltips")]
        public GameObject TooltipPanel;
        public Text TooltipMsg;

        [Header("Messages:")]
		//Population UI:
		public Text PopulationText; //a text that shows the faction's population

		//Message UI:
		public enum MessageTypes {Error, Info};
		public Text PlayerMessage;
		public float PlayerMessageReload = 3.0f; //how long does the player message shows for.
		float PlayerMessageTimer;

		//Peace Time UI:
		public GameObject PeaceTimePanel;
		public Text PeaceTimeText;

		[HideInInspector]
		public SelectionManager SelectionMgr;
		[HideInInspector]
		public BuildingPlacement PlacementMgr;
		[HideInInspector]
		public GameManager GameMgr;
		[HideInInspector]
		public ResourceManager ResourceMgr;
		[HideInInspector]
		public UnitManager UnitMgr;
        TaskManager TaskMgr;

		[System.Serializable]
		public class FactionSpritesVars
		{
			public string Code;
			public Sprite Sprite;
		}
		[System.Serializable]
		public class FactionUIVars
		{
			public Image Image;
			public FactionSpritesVars[] FactionSprites;
		}
		[Header("Custom Faction UI:")]
		public FactionUIVars[] FactionUI;

        void Awake ()
        {
            //assign the instance:
            if (instance == null)
                instance = this;
            else if (instance != this) //if we have another already active instance of this component
                Destroy(this); //destroy this instance then
        }

		void Start ()
		{
            //Hide all the task buttons/info UI objects at the start of the game
            HideSelectionInfoPanel();

            //get the scripts:
            GameMgr = GameManager.Instance;
            SelectionMgr = GameMgr.SelectionMgr;
            PlacementMgr = GameMgr.PlacementMgr;
            ResourceMgr = GameMgr.ResourceMgr;
            UnitMgr = GameMgr.UnitMgr;
            TaskMgr = GameMgr.TaskMgr;

            //default settings for tasks button.
            BaseTaskButton.GetComponent<TaskUI>().ID = 0;
            TaskButtons.Add(BaseTaskButton);
            BaseTaskButton.gameObject.SetActive(false);
            ActiveTaskButtons = 0;

            //hide the in proress tasks menu, multiple selection and single selection menus:
            MultipleSelectionIcons.Add(BaseMultipleSelectionIcon);
            BaseMultipleSelectionIcon.gameObject.SetActive(false);
            MultipleSelectionParent.gameObject.SetActive(false);
            SingleSelectionParent.gameObject.SetActive(false);

            //default settings for in progress tasks:
            FirstInProgressTaskButton.GetComponent<TaskUI>().ID = 0;
            BaseInProgressTaskButton.GetComponent<TaskUI>().ID = 1;
            InProgressTaskButtons.Add(FirstInProgressTaskButton);
            InProgressTaskButtons.Add(BaseInProgressTaskButton);

            //Hide the winning and loosing menu at the start of the game:
            WinningMenu.SetActive(false);
            LoosingMenu.SetActive(false);

            //Hide the pause menu as well:
            PauseMenu.SetActive(false);

            if (EnableHoverHealthBar == true) //if hover health bar is enabled.
            {
                //Hover health bar:
                //Hide the health bar canvas in the beginning:
                if (HoverHealthBarCanvas != null)
                {
                    HoverHealthBarCanvas.gameObject.SetActive(false);
                }
                HoverHealthBarActive = false;
                HoverSource = null;
            }

            if (FactionUI.Length > 0) {
				for (int i = 0; i < FactionUI.Length; i++) {
					if (FactionUI [i].Image) {
						bool Found = false;
						int j = 0;
						while (j < FactionUI [i].FactionSprites.Length && Found == false) {
                            if (GameMgr.Factions[GameManager.PlayerFactionID].TypeInfo != null) //makeing sure there's a valid faction type info assigned
                            {
                                if (FactionUI[i].FactionSprites[j].Code == GameMgr.Factions[GameManager.PlayerFactionID].TypeInfo.Code)
                                { //if the code matches
                                    FactionUI[i].Image.sprite = FactionUI[i].FactionSprites[j].Sprite; //use the custom UI
                                    Found = true;
                                }
                            }
							j++;
						}
					}
				}
			}
		}

		void Update () 
		{
			//player message timer:
			if (PlayerMessageTimer > 0) {
				PlayerMessageTimer -= Time.deltaTime;
			}
			if (PlayerMessageTimer < 0) {
				//Hide the player message:
				PlayerMessage.text = "";
				PlayerMessage.gameObject.SetActive (false);
			}
		}

        //a method tht updates the population UI count
        public void UpdatePopulationUI(int currPopulation, int maxPopulation)
        {
            if (PopulationText)
            {
                PopulationText.text = currPopulation.ToString() + "/" + maxPopulation.ToString();
            }
        }

        public void UpdatePopulationUI ()
		{
			if (PopulationText) {
				PopulationText.text = GameManager.Instance.Factions [GameManager.PlayerFactionID].GetCurrentPopulation().ToString () + "/" + GameMgr.Factions [GameManager.PlayerFactionID].GetMaxPopulation().ToString ();
			}
		}

		//Task buttons:

		//a method that hides all the task buttons.
		public void HideTaskButtons ()
		{
			if(TaskButtons.Count > 0)
			{
				//go through all the task buttons
				for(int i = 0; i < TaskButtons.Count; i++)
				{
					//hide the task buttons
					TaskButtons[i].gameObject.SetActive(false);
                    Image TaskImg = TaskButtons[i].gameObject.GetComponent<Image>();
                    TaskImg.color = Color.white;
                }
			}
			ActiveTaskButtons = 0;
		}

		//set the amount of task buttons:
		public void AddTaskButtons(int Amount)
		{
			if (TaskButtons.Count < ActiveTaskButtons+Amount) {
				int AmountNeeded = (Amount+ActiveTaskButtons) - TaskButtons.Count; //calculate how many task buttons we need:
				int j = TaskButtons.Count;

				//create new task buttons:
				for (int i = 0; i < AmountNeeded; i++) {
					GameObject NewTaskButton = Instantiate (BaseTaskButton.gameObject);
					//set the settings for the task button
					NewTaskButton.GetComponent<TaskUI> ().ID = j + i;
					//set the parent of the task button:
					NewTaskButton.transform.SetParent (TaskButtonsParent);
					NewTaskButton.transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
					TaskButtons.Add (NewTaskButton.GetComponent<Button>());
				}
			}

			ActiveTaskButtons += Amount;
        }

		public void SetTaskButtonParent (int i, int CategoryID)
		{
			if (i >= 0 && i < TaskButtons.Count) {
				//when category is -1, it means that there is no categories and there's only one parent object for all task buttons:
				if (CategoryID == -1 || UseTaskParentCategories == false) {
					TaskButtons [i].transform.SetParent (TaskButtonsParent);
				}
				//are we using task button categories?
				else if (UseTaskParentCategories == true) {
					//make sure that the task button exists:
					TaskButtons [i].transform.SetParent (TaskParentCategories [CategoryID]);
				}

				TaskButtons[i].transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
			}
		}


		//Multiple selection menu methods:

		//a method that hides all the multiple selection icons.
		public void HideMultipleSelectionIcons ()
		{
			if(MultipleSelectionIcons.Count > 0)
			{
				//go through all the multiple selection icons:
				for(int i = 0; i < MultipleSelectionIcons.Count; i++)
				{
					//hide each one of them.
					MultipleSelectionIcons[i].gameObject.SetActive(false);
				}
			}
		}

		//set the amount of multiple selection icons:
		public void SetMultipleSelectionIcons (int Amount)
		{
			//if we don't have enough
			if (MultipleSelectionIcons.Count < Amount) {
				Amount = Amount - MultipleSelectionIcons.Count; //determine how many we need:

				for (int i = 0; i < Amount; i++) {
					//create new selection icons
					GameObject NewMultipleSelectionIcon = Instantiate (BaseMultipleSelectionIcon.gameObject);
					//set their parent to the multiple selectin menu:
					NewMultipleSelectionIcon.transform.SetParent (MultipleSelectionParent);
					NewMultipleSelectionIcon.transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
					MultipleSelectionIcons.Add (NewMultipleSelectionIcon.GetComponent<Image>());
				}
			}
		}

		//In progress task buttons:

		//a method that hides all in progress task buttons:
		public void HideInProgressTaskButtons ()
		{
			if(InProgressTaskButtons.Count > 0)
			{
				//go through all of the existing in progress task buttons:
				for(int i = 0; i < InProgressTaskButtons.Count; i++)
				{
					//hide the inprogress task button:
					InProgressTaskButtons[i].gameObject.SetActive(false);
				}
			}
		}

		//set the amount of in progress task buttons:
		public void SetInProgressTaskButtons(int Amount)
		{
			//if we don't have enough in progress task buttons:
			if (InProgressTaskButtons.Count < Amount) {
				//set the amount of needed in progress task button:
				Amount = Amount - InProgressTaskButtons.Count;
				int j = InProgressTaskButtons.Count;

				for (int i = 0; i < Amount; i++) {
					//create a new in progress task button:
					GameObject NewInProgressTaskButton = Instantiate (BaseInProgressTaskButton.gameObject);
					NewInProgressTaskButton.GetComponent<TaskUI> ().ID = j + i;
                    //set the parent of the in progress task button
                    NewInProgressTaskButton.transform.SetParent (InProgressTaskButtonsParent);
					NewInProgressTaskButton.transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
					InProgressTaskButtons.Add (NewInProgressTaskButton.GetComponent<Button>());

				}
			}
		}

		//a method that hides the selection info panel:
		public void HideSelectionInfoPanel ()
		{
			MultipleSelectionParent.gameObject.SetActive (false);
			SingleSelectionParent.gameObject.SetActive (false);
			HideMultipleSelectionIcons ();
			HideInProgressTaskButtons ();
		}

		//Updating the currently selected building UI to show its info:
		public void UpdateBuildingUI (Building SelectedBuilding)
		{
			//show and hide the UI elements in the selection menu:
			MultipleSelectionParent.gameObject.SetActive (false);
			SingleSelectionParent.gameObject.SetActive (true);

			SelectionName.gameObject.SetActive (true);
			SelectionDescription.gameObject.SetActive (true);
			SelectionIcon.gameObject.SetActive (true);
			SelectionHealth.gameObject.SetActive (true);

			if (SelectedBuilding.IsFree() == false) {
				SelectionName.text = (GameMgr.Factions[SelectedBuilding.FactionID].TypeInfo != null) ? SelectedBuilding.GetName() + " (" + GameMgr.Factions [SelectedBuilding.FactionID].Name + " - " + GameMgr.Factions [SelectedBuilding.FactionID].TypeInfo.Name + ")" : SelectedBuilding.GetName() + " (" + GameMgr.Factions[SelectedBuilding.FactionID].Name+")";
			} else {
				SelectionName.text = SelectedBuilding.GetName() + " (Free Building)";
			}
			//If the building is being built then show how many builders there are:
			if (SelectedBuilding.WorkerMgr.currWorkers > 0 && SelectedBuilding.FactionID == GameManager.PlayerFactionID) {
				SelectionDescription.text = "Builders: " + SelectedBuilding.WorkerMgr.currWorkers.ToString () + "/" + SelectedBuilding.WorkerMgr.GetAvailableSlots().ToString() + "\n" + SelectedBuilding.GetDescription();
			} else {
				SelectionDescription.text = SelectedBuilding.GetDescription();
			}
			SelectionIcon.sprite = SelectedBuilding.GetIcon();

            //only show tasks if selected unit is from player team.
            if (SelectedBuilding.FactionID == GameManager.PlayerFactionID)
            {
                UpdateTaskPanel();

                //update the in progress tasks:
                UpdateInProgressTasksUI();
            }

            //update the building's health:
            UpdateFactionEntityHealthUI (SelectedBuilding);
		}

        //a method to update the task panel:
        public void UpdateTaskPanel ()
        {
            HideTaskButtons(); //first hide all current tasks.

            TaskLauncher TaskComp = null;
            APC APCComp = null;

            int LastTaskID = 0;

            AttackEntity AttackComp = null;

            if (SelectionMgr.SelectedBuilding != null) //if a building is selected:
            {
                if(SelectionMgr.SelectedBuilding.IsBuilt == true) //is the building already built? 
                    AttackComp = SelectionMgr.SelectedBuilding.AttackComp;

                //attack task:
                if (AttackComp != null && TaskMgr.AttackTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Attack, TaskMgr.AttackTaskCategory, TaskMgr.AttackTaskIcon);
                }

                //make sure the building belongs to the local player:
                if (SelectionMgr.SelectedBuilding.FactionID != GameManager.PlayerFactionID)
                    return;

                //Get the task component:
                TaskComp = SelectionMgr.SelectedBuilding.TaskLauncherComp;

                //Get the APC component (if there's one):
                APCComp = SelectionMgr.SelectedBuilding.APCComp;

                //Building related tasks here:

                //resource generation tasks
                if (SelectionMgr.SelectedBuilding.GeneratorComp)
                {
                    for(int i = 0; i < SelectionMgr.SelectedBuilding.GeneratorComp.GetGeneratorsLength(); i++) //go through all generators
                    {
                        ResourceGenerator.Generator g = SelectionMgr.SelectedBuilding.GeneratorComp.GetGenerator(i);
                        if (g.IsMaxAmountReached() == true) //if this generator reached the maximum amount already 
                        {
                            AddTaskButtons(1); //add a task button and add the generator's task
                            AddTask(ref LastTaskID, i, TaskManager.TaskTypes.ResourceGen, SelectionMgr.SelectedBuilding.GeneratorComp.GetTaskPanelCategory(), g.GetTaskIcon());
                        }
                    }
                }
            }
            else if (SelectionMgr.SelectedUnits.Count > 0) //if unit(s) are selected
            {
                //if the units belong to another faction
                if (SelectionMgr.SelectedUnits[0].FactionID != GameManager.PlayerFactionID)
                    return;

                //Get the task launcher component only if there's one unit selected
                if (SelectionMgr.SelectedUnits.Count == 1)
                {
                    TaskComp = SelectionMgr.SelectedUnits[0].TaskLauncherComp;
                    //Get the APC component (if there's one):
                    APCComp = SelectionMgr.SelectedUnits[0].APCComp;
                }

                //Unit related tasks here.
                bool CanMove = SelectionMgr.SelectedUnits[0].MovementComp.CanMove(); //can the player be moved? 
                AttackComp = SelectionMgr.SelectedUnits[0].AttackComp; //can the player attack? 

                Builder BuilderComp = SelectionMgr.SelectedUnits[0].BuilderComp; //can the player attack? 
                Converter ConverterComp = SelectionMgr.SelectedUnits[0].ConverterComp; //can the player convert? 
                Healer HealerComp = SelectionMgr.SelectedUnits[0].HealerComp; //can the player heal? 
                ResourceCollector CollectComp = SelectionMgr.SelectedUnits[0].CollectorComp;

                //Loop through the rest of the selected units
                if (SelectionMgr.SelectedUnits.Count > 1)
                {
                    int i = 1; //counter
                    while (i < SelectionMgr.SelectedUnits.Count && (BuilderComp != null || CanMove == true || AttackComp != null || ConverterComp != null || HealerComp != null || CollectComp != null))
                    {
                        //Checking the building tasks:
                        if (!SelectionMgr.SelectedUnits[i].BuilderComp && BuilderComp != null)
                        {
                            //Don't show the builder component on the tasks tab:
                            BuilderComp = null;
                        }
                        if (SelectionMgr.SelectedUnits[i].MovementComp.CanMove() == false && CanMove == true)
                        { //if the unit can't be moved then 
                          //don't show no mvt task
                            CanMove = false;
                        }
                        if (!SelectionMgr.SelectedUnits[i].AttackComp && AttackComp != null)
                        {
                            //Don't show the attack component on the tasks tab:
                            AttackComp = null;
                        }
                        if (!SelectionMgr.SelectedUnits[i].HealerComp && HealerComp != null)
                        {
                            //Don't show the heal component on the tasks tab:
                            HealerComp = null;
                        }
                        if (!SelectionMgr.SelectedUnits[i].ConverterComp && ConverterComp != null)
                        {
                            //Don't show the convert component on the tasks tab:
                            ConverterComp = null;
                        }
                        if (!SelectionMgr.SelectedUnits[i].CollectorComp && CollectComp != null)
                        {
                            //Don't show the collect component on the tasks tab:
                            CollectComp = null;
                        }

                        i++;
                    }
                }

                //movement task:
                if (CanMove == true && TaskMgr.MvtTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Mvt, TaskMgr.MvtTaskCategory, TaskMgr.MvtTaskIcon);
                }

                //collect task:
                if (CollectComp != null && TaskMgr.CollectTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Collect, TaskMgr.CollectTaskCategory, TaskMgr.CollectTaskIcon);
                }

                //attack task:
                if (AttackComp != null && TaskMgr.AttackTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Attack, TaskMgr.AttackTaskCategory, TaskMgr.AttackTaskIcon);
                }

                //heal task:
                if (HealerComp != null && TaskMgr.HealTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Heal, TaskMgr.HealTaskCategory, TaskMgr.HealTaskIcon);
                }

                //convert task:
                if (ConverterComp != null && TaskMgr.ConvertTaskIcon != null)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Convert, TaskMgr.ConvertTaskCategory, TaskMgr.ConvertTaskIcon);
                }

                //builder task:
                if (BuilderComp != null)
                {
                    if (TaskMgr.BuildTaskIcon != null)
                    { //if the builder task icon has been set then add another task for it
                        AddTaskButtons(PlacementMgr.AllBuildings.Count + 1); //Update the amont of the task buttons:
                    }
                    else
                    {
                        AddTaskButtons(PlacementMgr.AllBuildings.Count); //Update the amont of the task buttons:
                    }
                    for (int i = 0; i < PlacementMgr.AllBuildings.Count; i++)
                    {
                        AddTask(ref LastTaskID, i, TaskManager.TaskTypes.PlaceBuilding, PlacementMgr.AllBuildings[i].GetTaskPanelCategory(), PlacementMgr.AllBuildings[i].GetComponent<Building>().GetIcon());
                        TaskButtons[LastTaskID - 1].GetComponent<Image>().color = GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.HasReachedLimit(PlacementMgr.AllBuildings[i].GetCode()) ? Color.red : Color.white;
                    }

                    if (TaskMgr.BuildTaskIcon != null)
                    {
                        //builder component task button:
                        AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.Build, TaskMgr.BuildTaskCategory, TaskMgr.BuildTaskIcon);
                    }

                }

                if (SelectionMgr.SelectedUnits.Count == 1) //if only one unit is selected.
                {
                    //Multiple attacks:
                    if (SelectionMgr.SelectedUnits[0].MultipleAttackMgr)
                    {
                        //Only if we're allowed to switch the attack:
                        if (SelectionMgr.SelectedUnits[0].MultipleAttackMgr.CanSwitchAttacks() == true)
                        {
                            AddTaskButtons(SelectionMgr.SelectedUnits[0].MultipleAttackMgr.AttackEntities.Length - 1); //(why -1? because is already enabled! so we won't show it:)
                            for (int i = 0; i < SelectionMgr.SelectedUnits[0].MultipleAttackMgr.AttackEntities.Length; i++)
                            {
                                if (SelectionMgr.SelectedUnits[0].MultipleAttackMgr.AttackEntities[i].IsActive() == false)
                                {
                                    AddTask(ref LastTaskID, i, TaskManager.TaskTypes.AttackTypeSelection, SelectionMgr.SelectedUnits[0].MultipleAttackMgr.GetTaskCategory(), SelectionMgr.SelectedUnits[0].MultipleAttackMgr.AttackEntities[i].GetIcon());
                                    //if the attack is in cooldown mode:
                                    if (SelectionMgr.SelectedUnits[0].MultipleAttackMgr.AttackEntities[i].CoolDownActive == true)
                                    {
                                        Image TaskImg = TaskButtons[LastTaskID - 1].gameObject.GetComponent<Image>();
                                        Color ImgColor = new Color(TaskImg.color.r, TaskImg.color.g, TaskImg.color.b, 0.5f);
                                        TaskImg.color = ImgColor;
                                    }
                                }
                            }
                        }
                    }

                    //Wandering:
                    if (SelectionMgr.SelectedUnits[0].WanderComp) //can the unit wander? 
                    {
                        //add a new task button
                        AddTaskButtons(1);
                        Sprite WanderTaskSprite = (SelectionMgr.SelectedUnits[0].WanderComp.IsActive() == true) ? TaskMgr.DisableWanderIcon : TaskMgr.EnableWanderIcon;
                        AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.ToggleWander, TaskMgr.WanderTaskCategory, WanderTaskSprite);
                    }
                }
            }

            //Check if there's a task launcher:
            if (TaskComp != null)
            {
                //add slots for the tasks in the task launcher
                AddTaskButtons(TaskComp.TasksList.Count);

                //go through all tasks:
                //Because some tasks have upgrades (which will be represented by another task) we use this one to keep track of the task count
                for (int TasksCount = 0; TasksCount < TaskComp.TasksList.Count; TasksCount++)
                {
                    //make sure the task is available:
                    if (TaskMgr.IsTaskEnabled(TaskComp.TasksList[TasksCount].Code, TaskComp.FactionID, TaskComp.TasksList[TasksCount].IsAvailable) == true && TaskComp.CanManageTask())
                    {
                        //If this is a non - create unit task, if it's not active and not reached or this is simply a create unit task
                        if ((TaskComp.TasksList[TasksCount].TaskType != TaskManager.TaskTypes.CreateUnit && TaskComp.TasksList[TasksCount].Active == false && TaskComp.TasksList[TasksCount].Reached == false) || TaskComp.TasksList[TasksCount].TaskType == TaskManager.TaskTypes.CreateUnit)
                        {
                            //add task:
                            AddTask(ref LastTaskID, TasksCount, TaskComp.TasksList[TasksCount].TaskType, TaskComp.TasksList[TasksCount].TaskPanelCategory, TaskComp.TasksList[TasksCount].TaskIcon, TaskComp);

                            //if this is a unit creation task
                            if (TaskComp.TasksList[TasksCount].TaskType == TaskManager.TaskTypes.CreateUnit)
                            {
                                //set the color of the task depending on the limit for this unit type:
                                TaskButtons[LastTaskID - 1].GetComponent<Image>().color = (GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.HasReachedLimit(TaskComp.TasksList[TasksCount].UnitCreationSettings.Prefabs[0].GetCode())) ? Color.red : Color.white;
                            }
                        }
                    }
                }
            }

            if(APCComp != null) //if there's an APC component attached to the selected building/unit:
            {
                if (APCComp.IsEmpty() == false) //if there are units stored inside the APC
                {
                    if (APCComp.CanEject(true) == true) //if we're allowed to eject all units at once
                    {
                        AddTaskButtons(1);

                        //add task
                        AddTask(ref LastTaskID, 0, TaskManager.TaskTypes.APCEjectAll, APCComp.GetTaskCategory (true), APCComp.GetEjectAllUnitsIcon());
                    }

                    if (APCComp.CanEject(false) == true) //if we're allowed to eject single units
                    {
                        AddTaskButtons(APCComp.GetCount());
                        //go through all stored units
                        for (int i = 0; i < APCComp.GetCount(); i++)
                        {
                            //task for each unit
                            AddTask(ref LastTaskID, i, TaskManager.TaskTypes.APCEject, APCComp.GetTaskCategory (false), APCComp.GetStoredUnit(i).GetIcon());
                        }
                    }
                }
                //if there are still free slots in the APC and we can call units
                if (APCComp.IsFull() == false && APCComp.CanCallUnits() == true)
                {
                    AddTaskButtons(1);
                    AddTask(ref LastTaskID, -1, TaskManager.TaskTypes.APCCall, APCComp.GetCallUnitsTaskCategory(), APCComp.GetCallUnitsIcon());
                    ///call units task
                }
            }
        }

        //a method that adds a task to the task panel
        public void AddTask(ref int TaskButtonID, int TaskID, TaskManager.TaskTypes TaskType, int Category, Sprite Sprite, TaskLauncher TaskComp = null)
        {
            SetTaskButtonParent(TaskButtonID, Category);
            TaskButtons[TaskButtonID].gameObject.SetActive(true);
            TaskButtons[TaskButtonID].gameObject.GetComponent<Image>().sprite = Sprite;
            TaskButtons[TaskButtonID].gameObject.GetComponent<TaskUI>().TaskType = TaskType;
            TaskButtons[TaskButtonID].gameObject.GetComponent<TaskUI>().TaskSprite = Sprite;
            TaskButtons[TaskButtonID].gameObject.GetComponent<TaskUI>().ID = TaskID;
            TaskButtons[TaskButtonID].gameObject.GetComponent<TaskUI>().TaskComp = TaskComp;
            TaskButtonID++;
        }

        //a method that updates the building's in progress tasks:
        public void UpdateInProgressTasksUI()
        {
            HideInProgressTaskButtons();//first hide all current in progress tasks.

            TaskLauncher TaskComp = null;

            if (SelectionMgr.SelectedBuilding != null) //if a building is selected:
            {
                //make sure the building belongs to the local player:
                if (SelectionMgr.SelectedBuilding.FactionID != GameManager.PlayerFactionID)
                    return;

                //Get the task component:
                TaskComp = SelectionMgr.SelectedBuilding.TaskLauncherComp;
            }
            else if (SelectionMgr.SelectedUnits.Count == 1) //if there's one selected unit
            {
                //make sure the building belongs to the local player:
                if (SelectionMgr.SelectedUnits[0].FactionID != GameManager.PlayerFactionID)
                    return;

                //Get the task component:
                TaskComp = SelectionMgr.SelectedUnits[0].TaskLauncherComp;
            }

            //only continue if there's actually a task launcher:
            if (TaskComp != null)
            {
                if (TaskComp.TasksQueue.Count > 0) //if we actually have pending tasks:
                { 
                    SetInProgressTaskButtons(TaskComp.TasksQueue.Count); //add enough slots
                    
                    //go through all of them
                    for (int i = 0; i < TaskComp.TasksQueue.Count; i++)
                    {
                        //show the pending tasks:
                        InProgressTaskButtons[i].gameObject.SetActive(true);

                        //settings of the pending task:
                        InProgressTaskButtons[i].gameObject.GetComponent<TaskUI>().TaskType = TaskManager.TaskTypes.CancelPendingTask;
                        InProgressTaskButtons[i].gameObject.GetComponent<TaskUI>().TaskComp = TaskComp;

                        InProgressTaskButtons[i].gameObject.GetComponent<Image>().sprite = TaskComp.TasksList[TaskComp.TasksQueue[i].ID].TaskIcon;

                        float Value = 0.5f;

                        //if this is the first task in the tasks queue, then we have a progress bar for it, YAAAY!
                        if (i == 0)
                        {
                            Value += (((TaskComp.TasksList[TaskComp.TasksQueue[0].ID].ReloadTime - TaskComp.TaskQueueTimer) * 0.5f) / TaskComp.TasksList[TaskComp.TasksQueue[0].ID].ReloadTime);

                            //Update the task's timer bar:
                            float NewBarLength = (1-(TaskComp.TaskQueueTimer / TaskComp.TasksList[TaskComp.TasksQueue[0].ID].ReloadTime)) * FirstInProgressTaskBarEmpty.gameObject.GetComponent<RectTransform>().sizeDelta.x;

                            FirstInProgressTaskBarFull.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(NewBarLength, FirstInProgressTaskBarFull.gameObject.GetComponent<RectTransform>().sizeDelta.y);
                            FirstInProgressTaskBarFull.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(FirstInProgressTaskBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.x - (FirstInProgressTaskBarEmpty.gameObject.GetComponent<RectTransform>().sizeDelta.x - FirstInProgressTaskBarFull.gameObject.GetComponent<RectTransform>().sizeDelta.x) / 2.0f, FirstInProgressTaskBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.y, FirstInProgressTaskBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.z);
                        }

                        //the task icon's transparency shows the progress of the task. the more clear it is, the faster it will end.
                        Color ImgColor = new Color(InProgressTaskButtons[i].GetComponent<Image>().color.a, InProgressTaskButtons[i].GetComponent<Image>().color.g, InProgressTaskButtons[i].GetComponent<Image>().color.b, Value);
                        InProgressTaskButtons[i].GetComponent<Image>().color = ImgColor;
                    }
                }
            }
		}

		//Resource UI:

		//updating the resource UI:
		public void UpdateResourceUI (Resource SelectedResource)
		{
			if (SelectionHealthBarEmpty.gameObject != null && SelectionHealthBarFull.gameObject != null) {
				SelectionHealthBarFull.gameObject.SetActive (false);
				SelectionHealthBarEmpty.gameObject.SetActive (false);
			}

			//show and hide object in the selection menu:
			MultipleSelectionParent.gameObject.SetActive (false);
			SingleSelectionParent.gameObject.SetActive (true);

			SelectionName.gameObject.SetActive (true);
			SelectionDescription.gameObject.SetActive (true);
			SelectionIcon.gameObject.SetActive (true);
			SelectionHealth.gameObject.SetActive (true);

			//display the resource's info:
			SelectionName.text = SelectedResource.GetResourceName();
			SelectionHealth.text = "";
			SelectionDescription.text = "";

			if (SelectedResource.ShowAmount() == true) {
				if (SelectedResource.IsInfinite() == true) {
					SelectionHealth.text = "Infinite Amount";
				} else {
					SelectionHealth.text = "Amount: " + SelectedResource.GetAmount();
				}
			}
			if (SelectedResource.ShowCollectors() )
				SelectionDescription.text = "Collectors: "+SelectedResource.WorkerMgr.currWorkers.ToString()+"/"+SelectedResource.WorkerMgr.GetAvailableSlots().ToString();


			SelectionIcon.sprite = ResourceMgr.ResourcesInfo[ResourceMgr.GetResourceID(SelectedResource.GetResourceName())].TypeInfo.Icon;

		}

		//update the unit UI to show its info:
		public void UpdateUnitUI (Unit SelectedUnit)
		{
			if (SelectionMgr.SelectedUnits.Count == 1) {
				//if we're selecing only one unit
				MultipleSelectionParent.gameObject.SetActive (false);
				SingleSelectionParent.gameObject.SetActive (true);

				//Showing the unit's info:
				SelectionName.gameObject.SetActive (true);
				SelectionDescription.gameObject.SetActive (true);
				SelectionIcon.gameObject.SetActive (true);
				SelectionHealth.gameObject.SetActive (true);
				if (SelectedUnit.IsFree() == false) {
                    SelectionName.text = (GameMgr.Factions[SelectedUnit.FactionID].TypeInfo != null) ? SelectedUnit.GetName() + " (" + GameMgr.Factions[SelectedUnit.FactionID].Name + " - " + GameMgr.Factions[SelectedUnit.FactionID].TypeInfo.Name + ")" : SelectedUnit.GetName() + " (" + GameMgr.Factions[SelectedUnit.FactionID].Name;
				} else {
					SelectionName.text = SelectedUnit.GetName() + " (Free Unit)";
				}
				SelectionDescription.text = SelectedUnit.GetDescription();
				if (SelectedUnit.APCComp) {
					SelectionDescription.text += "\nCapacity: " + SelectedUnit.APCComp.GetCount().ToString() + "/" + SelectedUnit.APCComp.GetCapacity().ToString();
				}
                if(ShowPopulationSlots == true)
                    SelectionDescription.text += "\n<b>Population Slots:</b> " + SelectedUnit.GetPopulationSlots().ToString();
                SelectionIcon.sprite = SelectedUnit.GetIcon();


			} else if(SelectionMgr.SelectedUnits.Count > 1) { //if more than one unit has been selected.
				//show the multiple selection menu:
				HideMultipleSelectionIcons ();
				SingleSelectionParent.gameObject.SetActive (false);
				MultipleSelectionParent.gameObject.SetActive (true);

				SetMultipleSelectionIcons (SelectionMgr.SelectedUnits.Count);

				for (int i = 0; i < SelectionMgr.SelectedUnits.Count; i++) {
					//only each seleced unit's icon.
					MultipleSelectionIcons [i].gameObject.SetActive (true);
					MultipleSelectionIcons [i].sprite = SelectionMgr.SelectedUnits[i].GetIcon();
				}
			}

			//only show tasks if selected unit is from player team.
			if(SelectedUnit.FactionID == GameManager.PlayerFactionID) 
			{
				UpdateTaskPanel ();

                //update the in progress tasks:
                UpdateInProgressTasksUI();
            }

            UpdateFactionEntityHealthUI(SelectedUnit);
		}

        //updates the select faction entity (unit/building) health bar:
        public void UpdateFactionEntityHealthUI(FactionEntity factionEntity)
        {
            //if multiple units are selected then update the multiple units health UI
            if (SelectionMgr.SelectedUnits.Count > 1)
            {
                UpdateMultipleUnitsHealthUI();
                return;
            }

            //update the faction entity health UI in the selection panel
            SelectionHealth.text = factionEntity.EntityHealthComp.CurrHealth.ToString() + "/" + factionEntity.EntityHealthComp.MaxHealth.ToString();

            if (SelectionHealthBarFull.gameObject != null && SelectionHealthBarEmpty.gameObject != null)
            {
                SelectionHealthBarFull.gameObject.SetActive(true);
                SelectionHealthBarEmpty.gameObject.SetActive(true);
                //Update the health bar:
                float NewBarLength = (factionEntity.EntityHealthComp.CurrHealth / (float)factionEntity.EntityHealthComp.MaxHealth) * SelectionHealthBarEmpty.gameObject.GetComponent<RectTransform>().sizeDelta.x;
                SelectionHealthBarFull.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(NewBarLength, SelectionHealthBarFull.gameObject.GetComponent<RectTransform>().sizeDelta.y);
                SelectionHealthBarFull.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(SelectionHealthBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.x - (SelectionHealthBarEmpty.gameObject.GetComponent<RectTransform>().sizeDelta.x - SelectionHealthBarFull.gameObject.GetComponent<RectTransform>().sizeDelta.x) / 2.0f, SelectionHealthBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.y, SelectionHealthBarEmpty.gameObject.GetComponent<RectTransform>().localPosition.z);
            }
        }

		//Updates the selected units health bar:
		public void UpdateMultipleUnitsHealthUI()
		{
				//Loop through the selected units sprites.
				for (int i = 0; i < SelectionMgr.SelectedUnits.Count; i++) {

					if (MultipleSelectionIcons [i].gameObject.GetComponent<TaskUI> ()) {
						if (MultipleSelectionIcons [i].gameObject.GetComponent<TaskUI> ().EmptyBar != null && MultipleSelectionIcons [i].gameObject.GetComponent<TaskUI> ().FullBar != null) {
							Image EmptyBar = MultipleSelectionIcons [i].gameObject.GetComponent<TaskUI> ().EmptyBar;
							Image FullBar = MultipleSelectionIcons [i].gameObject.GetComponent<TaskUI> ().FullBar;

							//Update the health bar:
							float NewBarLength = (SelectionMgr.SelectedUnits[i].HealthComp.CurrHealth / (float)SelectionMgr.SelectedUnits[i].HealthComp.MaxHealth) * EmptyBar.gameObject.GetComponent<RectTransform> ().sizeDelta.x;
							FullBar.gameObject.GetComponent<RectTransform> ().sizeDelta = new Vector2 (NewBarLength, FullBar.gameObject.GetComponent<RectTransform> ().sizeDelta.y);
							FullBar.gameObject.GetComponent<RectTransform> ().localPosition = new Vector3 (EmptyBar.gameObject.GetComponent<RectTransform> ().localPosition.x - (EmptyBar.gameObject.GetComponent<RectTransform> ().sizeDelta.x - FullBar.gameObject.GetComponent<RectTransform> ().sizeDelta.x) / 2.0f, EmptyBar.gameObject.GetComponent<RectTransform> ().localPosition.y, EmptyBar.gameObject.GetComponent<RectTransform> ().localPosition.z);
						}
					}
				}
			}

		//a method that contrls showing messags to the player:
		public void ShowPlayerMessage(string Text, MessageTypes Type)
		{
			PlayerMessage.gameObject.SetActive (true);

			PlayerMessage.text = Text;

			PlayerMessageTimer = PlayerMessageReload;
		}

		//Peace time UI:
		public void UpdatePeaceTimeUI (float PeaceTime)
		{
			if (PeaceTimeText != null) {
				if (PeaceTime > 0) {
					PeaceTimePanel.gameObject.SetActive (true);

					int Seconds = Mathf.RoundToInt (PeaceTime);
					int Minutes = Mathf.FloorToInt (Seconds / 60.0f);

					Seconds = Seconds - Minutes * 60;

					string MinutesText = "";
					string SecondsText = "";

					if (Minutes < 10) {
						MinutesText = "0" + Minutes.ToString ();
					} else {
						MinutesText = Minutes.ToString ();
					}

					if (Seconds < 10) {
						SecondsText = "0" + Seconds.ToString ();
					} else {
						SecondsText = Seconds.ToString ();
					}

					PeaceTimeText.text = MinutesText + ":" + SecondsText;
				} else {
					PeaceTimePanel.gameObject.SetActive (false);
				}
			}
		}

        //Pause menu:
        public void TogglePause ()
        {
            if(GameManager.GameState == GameStates.Paused) //if the game was paused
            {
                //unpause it:
                PauseMenu.SetActive(false); //Hide the pause menu:
                //game is running again:
                GameManager.GameState = GameStates.Running;
            }
            else if(GameManager.GameState == GameStates.Running) //if the game is running
            {
                //pause it:
                PauseMenu.SetActive(true); //Show the pause menu
                GameManager.GameState = GameStates.Paused;
            }
        }

        //Hover health bar:
        public void TriggerHoverHealthBar(bool Enable, SelectionEntity Source, float Height)
        {
            if (Enable == true) //enabling the hover health bar
            {
                if (Source != null && HoverHealthBarActive == false) //make sure we have a valid source and the hover health bar is inactive
                {
                    HoverSource = Source; //set the new source
                                          //enable the hover health bar:
                    HoverHealthBarCanvas.gameObject.SetActive(true);
                    //make the canvas a child object of the source obj:
                    HoverHealthBarCanvas.transform.SetParent(Source.GetMainObject().transform, true);
                    HoverHealthBarActive = true;
                    //set the new health bar canvas position, the height is specified in either the Unit or Building component.
                    HoverHealthBarCanvas.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, Height, 0.0f);
                }
            }
            else //disabling it
            {
                //make sure that the source is disabling it (or there is no source):
                if (HoverSource == null || HoverSource == Source)
                {
                    //disable the hover health bar:
                    HoverHealthBarCanvas.gameObject.SetActive(false);
                    //no transform parent anymore
                    HoverHealthBarCanvas.transform.SetParent(null, true);

                    HoverHealthBarActive = false;
                }
            }
        }

        //check if a selection obj is actually the source of the hover health bar
        public bool IsHoverSource(SelectionEntity Obj)
        {
            if (Obj == null)
                return false;
            if (Obj == HoverSource)
                return true;
            return false;
        }

        //the method that updates the health bar of the unit/building that the player has their mouse on.
        public void UpdateHoverHealthBar(float Health, float MaxHealth)
        {
            //only proceed if there's actually a health bar canvas
            if (HoverHealthBarCanvas != null)
            {
                if (Health < 0.0f)
                    Health = 0.0f; //Health can't be below 0

                if (HoverHealthBarFull.gameObject != null && HoverHealthBarEmpty.gameObject != null) //make sure we have both sprites
                {
                    //Update the health bar:
                    float NewBarLength = (Health / MaxHealth) * HoverHealthBarEmpty.sizeDelta.x;
                    HoverHealthBarFull.sizeDelta = new Vector2(NewBarLength, HoverHealthBarFull.sizeDelta.y);
                    HoverHealthBarFull.localPosition = new Vector3(HoverHealthBarEmpty.localPosition.x - (HoverHealthBarEmpty.sizeDelta.x - HoverHealthBarFull.sizeDelta.x) / 2.0f, HoverHealthBarEmpty.localPosition.y, HoverHealthBarEmpty.localPosition.z);
                }
            }
        }

        //Tooltip:

        //a method to enable the tooltip panel:
        public void ShowTooltip(string Msg)
        {
            TooltipPanel.SetActive(true); //activate the panel

            TooltipMsg.text = Msg; //display the message
        }

        //a method to hide the tooltip panel:
        public void HideTooltip()
        {
            TooltipPanel.SetActive(false); //hide the panel

            TooltipMsg.text = ""; //remove the last message
        }

    }
}