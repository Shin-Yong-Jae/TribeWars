using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;
using UnityEngine.Events;

/* Task Launcher script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class TaskLauncher : MonoBehaviour
    {
        //This component can be attached to units and buildings only.
        public enum TaskHolders { Unit, Building};
        [HideInInspector]
        public TaskHolders TaskHolder;

        //unique code for each task launcher:
        public string Code = "unique_task_launcher_code";

        //Components that the Task Launcher is attached to.
        [HideInInspector]
        public Building RefBuilding;
        [HideInInspector]
        public Unit RefUnit;

        public float MinTaskHealth = 70.0f; //minimum health required in order to launch/complete a task. 

        public int MaxTasks = 4; //The amount of maximum tasks that this component can handle at the same time.

        public enum AllowedTaskTypes {CreateUnit, Destroy, CustomTask, buildingUpgrade, unitUpgrade};

        [System.Serializable]
        public class TasksVars
        {
            //Unique code for every task:
            public string Code = "unique_task_code";

            //Can this task be used only by a specific faction?
            public bool FactionSpecific = false;
            public string FactionCode = "Faction001";

            public string Description = "describe your task here"; //description shown in the task panel when hovering over the task button.
            [HideInInspector]
            public TaskManager.TaskTypes TaskType = TaskManager.TaskTypes.CreateUnit; //the type of the task.
            public AllowedTaskTypes AllowedTaskType = AllowedTaskTypes.CreateUnit; //so that only allowed task types are entered in the inspector
            public int TaskPanelCategory = 0; //if you are using different categories in the task panel then assign this for each task.
            public Sprite TaskIcon; //the icon shown in the tasks panel
            //Timers:
            public float ReloadTime = 3.0f; //how long does the task last?

            public ResourceManager.Resources[] RequiredResources = new ResourceManager.Resources[0]; //Resources required to complete this task.

            public AudioClip TaskCompletedAudio; //Audio clip played when the task is completed.

            public bool UseOnce = false; //Can this task only be once used?
            public bool useOnceOnAllInstances = false; //if the above option is selected and this one is enabled: then the task will be only usable once for all active instances. 

            public bool _isAvailable = true;
            public bool IsAvailable
            {
                get { return _isAvailable; }
                set { _isAvailable = value; }
            }

            public TaskManager.UnitCreationTask UnitCreationSettings = new TaskManager.UnitCreationTask(); //will be shown only in case the task type is a unit creation.

            public UnitUpgrade unitUpgrade; //will be shown only in  case the task type is a unit upgrade task.
            public BuildingUpgrade buildingUpgrade; //will be shown only in case the task type is a unit upgrade task.

            [SerializeField]
            private int upgradeTargetID = 0; //the upgrade target ID, since a unit/building upgrade component allows multiple upgrade possibilities
            public int GetUpgradeTargetID() { return upgradeTargetID; }

            //unlocking other tasks:
            public string[] tasksToUnlock; //an array of task codes that get unlocked once the task is completed.
            
            [HideInInspector]
            public bool Active = false; //is this task currently active?
            [HideInInspector]
            public bool Reached = false; //has this task been done? 

            //Events: Besides the custom delegate events, you can directly use the event triggers below to further customize the behavior of the tasks:
            public UnityEvent TaskLaunchEvent;
            public UnityEvent TaskCompleteEvent;
            public UnityEvent TaskCancelEvent;
        }
        public List<TasksVars> TasksList = new List<TasksVars>(); //all tasks go here.

        //a list of the pending tasks:
        [System.Serializable]
        public class TasksQueueInfo
        {
            public int ID = -1; //the task ID.
        }
        [HideInInspector]
        public List<TasksQueueInfo> TasksQueue = new List<TasksQueueInfo>(); //this is the task's queue which holds all the pending tasks
        [HideInInspector]
        public float TaskQueueTimer = 0.0f; //this is the task's timer. when it's done, one task out of the queue is done.

        //Audio:
        public AudioClip LaunchTaskAudio; //Audio played when a new building task is launched.
        public AudioClip DeclinedTaskAudio; //When the task is declined due to lack of resources, the fact that the maximum in progress task has been reached or the min task health is not present. 

        //Faction info:
        [HideInInspector]
        public int FactionID;
        public FactionManager FactionMgr;
        
        //Other components:
        GameManager GameMgr;
        UIManager UIMgr;
        SelectionManager SelectionMgr;
        ResourceManager ResourceMgr;
        TaskManager taskMgr;

        private bool isActive = false; //is the task launcher active or not?

        //Used for the custom editor:
        [HideInInspector]
        public int TaskID; //Current task ID that the user is configuring.
        [HideInInspector]
        public int TabID; //Current tab that the user is viewing

        //Called when the building/unit is ready to initialize the tasks
        public void OnTasksInit()
        {
            //get the building/unit components
            RefUnit = gameObject.GetComponent<Unit>();
            RefBuilding = gameObject.GetComponent<Building>();

            //we expect the Task Launcher to be attached to either a building or a unit
            Assert.IsTrue(RefUnit != null || RefBuilding != null);

            TaskHolder = (RefUnit != null) ? TaskHolders.Unit : TaskHolders.Building; //set the task holder.

            //Get the other components:
            GameMgr = GameManager.Instance;
            UIMgr = GameMgr.UIMgr;
            SelectionMgr = GameMgr.SelectionMgr;
            ResourceMgr = GameMgr.ResourceMgr;
            taskMgr = TaskManager.instance;

            SetFactionInfo();

            FactionMgr.TaskLaunchers.Add(this); //add the task launcher here.

            SetTaskTypes();

            isActive = true;

            //Launch the delegate event:
            if (GameMgr.Events)
                GameMgr.Events.OnTaskLauncherAdded(this);

            foreach (TasksVars task in TasksList)
                task.ReloadTime /= GameManager.Instance.GetSpeedModifier(); //apply the speed modifier for the reload time
        }

        //a method to determine whether the task holder is capable of launching a task:
        public bool CanManageTask ()
        {
            if (GetTaskHolderHealth() < MinTaskHealth) //if the task holder doesn't have enough health points
                return false; //can't launch task

            if(TaskHolder == TaskHolders.Unit) //if the task holder is a unit
            {
                return RefUnit.HealthComp.IsDead() == false; //make sure the unit is not dead and has enough health
            }
            else //if not, make sure the building is built, not destroyed, not upgrading and has enough health.
            {
                return RefBuilding.IsBuilt == true && RefBuilding.HealthComp.IsDestroyed == false;
            }
        }

        //a method to get the faction ID of the task holder:
        public void SetFactionInfo ()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                FactionID = RefUnit.FactionID;
                FactionMgr = RefUnit.FactionMgr; 
            }
            else
            {
                FactionID = RefBuilding.FactionID;
                FactionMgr = RefBuilding.FactionMgr;
            }
        }

        //a method to check if the task holder is selected:
        public bool IsTaskHolderSelected ()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                return SelectionMgr.SelectedUnits.Contains(RefUnit);
            }
            else
            {
                return SelectionMgr.SelectedBuilding == RefBuilding;
            }
        }

        //get the task holder's health
        public float GetTaskHolderHealth()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                return RefUnit.HealthComp.CurrHealth;
            }
            else
            {
                return RefBuilding.HealthComp.CurrHealth;
            }
        }

        //a method to get the spawn position for newly created units:
        public Vector3 GetSpawnPosition (LayerMask unitNavMeshLayerMask)
        {
            if(TaskHolder == TaskHolders.Building) //if the task holder is a building
            {
                return RefBuilding.GetSpawnPosition(unitNavMeshLayerMask);
            }
            else //if this is a unit
            {
                return transform.position; //return the unit's position
            }
        }

        void Update()
        {
            //only if the task launcher is active
            if (isActive == true)
            {
                if (CanManageTask() == true) //can the task holder manage tasks now?
                {
                    if (TasksQueue.Count > 0) //if there are pending tasks
                    {
                        //keep updating them:
                        UpdateTasks();
                    }
                }
            }
        }

        //Setting the task types will help factions pick the task that they need:
        void SetTaskTypes()
        {
            if (TasksList.Count > 0)
            { //if the building actually has tasks:
                int i = 0;
                while (i < TasksList.Count)
                {
                    //if the faction is controlled by the player in a single player or a multiplayer game:
                    if (FactionID == GameManager.PlayerFactionID)
                    {
                        //if the task is faction specific and it doesn't match with the task launcher's faction then remove it.
                        if (TasksList[i].FactionSpecific == true && (GameMgr.Factions[FactionID].TypeInfo == null || TasksList[i].FactionCode != GameMgr.Factions[FactionID].TypeInfo.Code))
                        {
                            TasksList.RemoveAt(i);
                            continue;
                        }
                    }

                    //if not, then check the status of availability of the task from the task manager:
                    TasksList[i].IsAvailable = taskMgr.IsTaskEnabled(TasksList[i].Code, FactionID, TasksList[i].IsAvailable);
                    i++;
                }
            }
        }

        //method called when the task launcher has pending tasks:
        void UpdateTasks()
        {
            //if the task timer is still going and we are not using the god mode
            if (TaskQueueTimer > 0 && GodMode.Enabled == false)
            {
                TaskQueueTimer -= Time.deltaTime;

                if(IsTaskHolderSelected())
                    UIMgr.UpdateInProgressTasksUI();
            }
            //till it stops:
            else
            {
                int completedTaskID = TasksQueue[0].ID;

                if (TasksQueue.Count > 0)
                    TasksQueue.RemoveAt(0);// Remove this task.

                //play the task complete audio if this is the player's faction
                if (TasksList[completedTaskID].TaskCompletedAudio != null && FactionID == GameManager.PlayerFactionID)
                {
                    AudioManager.PlayAudio(gameObject, TasksList[completedTaskID].TaskCompletedAudio, false); //Play the audio clip
                }

                //delegate event:
                if (GameMgr.Events)
                    GameMgr.Events.OnTaskCompleted(this, completedTaskID, 0);

                //Unity event:
                TasksList[completedTaskID].TaskCompleteEvent.Invoke();

                OnTaskCompleted(completedTaskID);

                if(IsTaskHolderSelected())
                {
                    //update the selection panel UI to show that this task is no longer in progress.
                    UIMgr.UpdateTaskPanel();
                    UIMgr.UpdateInProgressTasksUI();
                }

                if (TasksQueue.Count > 0)
                {
                    //if there are more tasks in the queue
                    TaskQueueTimer = TasksList[TasksQueue[0].ID].ReloadTime; //set the reload for the next task and start over.
                }
            }
        }

        //a method called when a normal task (not an upgrade one) is complete:
        void OnTaskCompleted(int taskID)
        {
            switch (TasksList[taskID].TaskType) //type of the task that has been completed.
            {
                //unit creation:
                case TaskManager.TaskTypes.CreateUnit:
                    //Randomly pick a prefab to produce:
                    Unit UnitPrefab = TasksList[taskID].UnitCreationSettings.Prefabs[Random.Range(0, TasksList[taskID].UnitCreationSettings.Prefabs.Count)];
                    UnitManager.CreateUnit(UnitPrefab, GetSpawnPosition(UnitPrefab.GetComponent<NavMeshAgent>().areaMask), FactionID, RefBuilding);

                    break;

                //destroying the task holder
                case TaskManager.TaskTypes.Destroy:
                    DestroyTaskHolder();
                    break;

                //unit upgrade task:
                case TaskManager.TaskTypes.unitUpgrade:
                    UpgradeManager.instance.LaunchUpgrade(TasksList[taskID].unitUpgrade, TasksList[taskID].GetUpgradeTargetID()); //launch building upgrad
                    break;

                //building upgrade task:
                case TaskManager.TaskTypes.buildingUpgrade:
                    UpgradeManager.instance.LaunchUpgrade(TasksList[taskID].buildingUpgrade, TasksList[taskID].GetUpgradeTargetID()); //launch building upgrade
                    break;
            }

            //see if the task can unlock other tasks:
            if(TasksList[taskID].tasksToUnlock.Length > 0)
            {
                //go through all the task codes to unlock and request to unlock them from the task manager:
                foreach (string code in TasksList[taskID].tasksToUnlock)
                    taskMgr.ToggleTask(code, FactionID, true);
            }
        }

        //a method to destroy the task holder:
        public void DestroyTaskHolder ()
        {
            if(TaskHolder == TaskHolders.Building)
            {
                //if this building is selected:
                if(SelectionMgr.SelectedBuilding == RefBuilding)
                    SelectionMgr.DeselectBuilding(); //deselect it.

                //Destroy building:
                RefBuilding.HealthComp.DestroyFactionEntity(false);
            }
            else
            {
                //deselect if selected:
                if (SelectionMgr.SelectedUnits.Contains(RefUnit))
                    SelectionMgr.DeselectUnit(RefUnit);

                //destroy the unit.
                RefUnit.HealthComp.DestroyFactionEntity(false);
            }
        }

        //a method that cancels all in progress tasks when called:
        public void CancelAllInProgressTasks ()
        {
            while(0 < TasksQueue.Count)
                CancelInProgressTask(0);
        }

        //Cancel a task in progress:
        public void CancelInProgressTask(int ID)
        {
            //make sure the task ID is valid:
            if (TasksQueue.Count > ID && ID >= 0)
            {
                //If it's a task that produces units, then make sure we empty a slot in the population count:
                if (TasksList[TasksQueue[ID].ID].TaskType == TaskManager.TaskTypes.CreateUnit)
                {
                    UIMgr.GameMgr.Factions[FactionID].UpdateCurrentPopulation(-TasksList[TasksQueue[ID].ID].UnitCreationSettings.Prefabs[0].GetPopulationSlots()); //update the population slots
                    if (GameManager.PlayerFactionID == FactionID)
                    {
                        UIMgr.UpdatePopulationUI();
                    }

                    //update the limits list:
                    FactionMgr.UpdateLimitsList(TasksList[TasksQueue[ID].ID].UnitCreationSettings.Prefabs[0].GetCode(), false);
                }
                ResourceMgr.GiveBackResources(TasksList[TasksQueue[ID].ID].RequiredResources, FactionID); //Give back the task resources.
            }

            //custom events
            if (GameMgr.Events)
                GameMgr.Events.OnTaskCanceled(this, TasksQueue[ID].ID, ID);

            //Unity event:
            TasksList[TasksQueue[0].ID].TaskCancelEvent.Invoke();

            TasksList[TasksQueue[ID].ID].Active = false; //task is no longer active.

            //if the task was supposed to be used once but is cancelled:
            if (TasksList[TasksQueue[ID].ID].UseOnce == true)
            {
                TasksList[TasksQueue[ID].ID].IsAvailable = true; //make it available again.
                if (TasksList[TasksQueue[ID].ID].useOnceOnAllInstances == true) //if this was marked as usable once for all instances.
                {
                    //enable task again:
                    taskMgr.ToggleTask(TasksList[TasksQueue[ID].ID].Code, FactionID, true);
                }
            }

            TasksQueue.RemoveAt(ID);// Remove this task:

            if (ID == 0 && TasksQueue.Count > 0)
            {
                //If it's the first task in the queue, reload the timer for the next task:
                TaskQueueTimer = TasksList[TasksQueue[0].ID].ReloadTime;
            }

            UIMgr.UpdateTaskPanel();
            UIMgr.UpdateInProgressTasksUI();

        }
    }
}
