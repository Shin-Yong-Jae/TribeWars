using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Upgrade Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public abstract class NPCUpgradeManager<T> : NPCComponent
    {
        //have timer that checks upgrade tasks
        //a field to prioritize unit or building upgrade over each other.

        //a structure that serves to hold the task launcher and the task ID of a unit/building upgrade task
        protected struct UpgradeTask
        {
            public TaskLauncher taskLauncher;
            public int taskID;
        }

        protected TaskManager.TaskTypes upgradeTaskType = TaskManager.TaskTypes.Null; //the task type that we're looking for in task launchers is held here.

        protected List<UpgradeTask> upgradeTasks = new List<UpgradeTask>(); //a list that holds the upgrade task infos.

        public bool autoUpgrade = true; //if enabled, then this component will launch task upgrade automatically
        public FloatRange timerReloadRange = new FloatRange(5.0f, 10.0f); //the timer reload (in seconds) for which upgrade tasks are checked and possibily launched
        protected float timer;

        //the acceptance range adds some randomness to NPC factions launching upgrade tasks.
        //each time a random float between 0.0f and 1.0f will be generated and if it is below a random value chosen from the below range...
        //...then the upgrade will be chosen. So this means that 0.0f -> upgrade will never be launched and 1.0f -> upgrade will always be launched.
        public FloatRange acceptanceRange = new FloatRange(0.5f, 0.8f);

        public bool upgradeOnDemand = true; //can other components request launching an upgrade task?

        //is this component active or in idle mode?
        private bool isActive = false;

        public void Activate()
        {
            isActive = true;
        }

        //other components
        private TaskManager taskMgr;

        void Start ()
        {
            taskMgr = TaskManager.instance;

            //update the upgrade task type:
            UpdateUpgradeTaskType();

            //initially, this component is active:
            Activate();

            //start the timer:
            timer = timerReloadRange.getRandomValue();

            //start listening to the delegate events
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved += OnTaskLauncherRemoved;
        }

        void OnDestroy()
        {
            //stop listening to the delegate events:
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved -= OnTaskLauncherRemoved;
        }

        //updates the upgrade task type
        private void UpdateUpgradeTaskType ()
        {
            if (typeof(T) == typeof(Unit))
                upgradeTaskType = TaskManager.TaskTypes.unitUpgrade;
            else if (typeof(T) == typeof(Building))
                upgradeTaskType = TaskManager.TaskTypes.buildingUpgrade;
            else
                Debug.LogError("[NPC Upgrade Manager] This component can only be assigned to upgrade Unit or Building types.");
        }

        void OnTaskLauncherAdded(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a new task launcher has been added
        {
            if (taskLauncher.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
            {
                UpdateUpgradeTasks(taskLauncher, true); //update upgrade tasks
            }
        }

        void OnTaskLauncherRemoved(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a task launcher has been removed
        {
            if (taskLauncher.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
            {
                UpdateUpgradeTasks(taskLauncher, false); //update upgrade tasks
            }
        }

        //a method that is given a task in a task launcher and an upgrade component and that decides if the task handles that upgrade
        protected abstract bool IsUpgradeMatch(TaskLauncher taskLauncher, int taskID, Upgrade<T> upgrade);

        //whenever a task launcher is added or removed, this method will be called to add/remove it to/from the upgrade tasks list:
        public void UpdateUpgradeTasks (TaskLauncher taskLauncher, bool add, Upgrade<T> upgrade = null)
        {
            //if taskID is set to -1 and add = false, then all the tasks with the assigned task launcher will be removed
            //if taskID >= 0 is valid, then only the upgrade task with taskID in the task launcher will be removed.

            if (add == false) //if this task launcher is getting removed.
            {
                int i = 0;
                //go through all registerd task launchers in this component
                while (i < upgradeTasks.Count)
                {
                    if (upgradeTasks[i].taskLauncher == taskLauncher || (upgrade != null && IsUpgradeMatch(upgradeTasks[i].taskLauncher, upgradeTasks[i].taskID, upgrade) == true)) //if the task launcher matches:
                                                                                                                                                                                    //remove it:
                        upgradeTasks.RemoveAt(i);
                    else
                        i++; //move on
                }
            }
            else if (taskLauncher.TasksList.Count > 0) //if we're adding tasks and this task launcher has tasks.
            {
                //loop through the task launcher's task
                for (int taskID = 0; taskID < taskLauncher.TasksList.Count; taskID++)
                {
                    //if this is the upgrade task that we're looking for.
                    if (taskLauncher.TasksList[taskID].TaskType == upgradeTaskType)
                    {
                        //go ahead and add it:
                        UpgradeTask newUpgradeTask = new UpgradeTask
                        {
                            taskLauncher = taskLauncher,
                            taskID = taskID
                        };
                        //add it to the list:
                        upgradeTasks.Add(newUpgradeTask);

                        //and activate the upgrade manager:
                        Activate();
                    }
                }
            }
        }

        void Update()
        {
            //if the upgrade manager is active:
            if (isActive == true && autoUpgrade == true)
            {
                //upgrade timer:
                if (timer > 0)
                    timer -= Time.deltaTime;

                else //if the timer is through
                {
                    timer = timerReloadRange.getRandomValue(); //reload timer

                    isActive = false; //assume that the upgrade manager has finished its job with the current registerd upgrade tasks

                    //go through the upgrade tasks if there are any:
                    if(upgradeTasks.Count > 0)
                    {
                        isActive = true; //there's still upgrade tasks so we will want to check this again

                        foreach (UpgradeTask tu in upgradeTasks)
                        {
                            //request to launch this upgrade
                            OnUpgradeLaunchRequest(tu.taskLauncher, tu.taskID, true);
                        }
                    }
                }
            }
        }

        //a method used to request to launch an upgrade task, returns true if the upgrade task is launched, false if not
        public bool OnUpgradeLaunchRequest(TaskLauncher taskLauncher, int taskID, bool auto)
        {
            //if this attempt is done automatically (from the NPC Unit Creator itself) and the regulator doesn't allow it
            if (auto == true && autoUpgrade == false)
            {
                return false; //do not proceed.
            }

            //if this has been requested from another NPC component and the regulator doesn't allow it
            if (auto == false && upgradeOnDemand == false)
            {
                return false; //do not proceed.
            }

            //randomly decide (based on input values) if this would be accepetd or not:
            if (Random.value <= acceptanceRange.getRandomValue())
            {

                //attempt to launch the task
                TaskManager.AddTaskMsg addTaskMsg = taskMgr.AddTask(taskLauncher, taskID, upgradeTaskType);

                //TO BE ADDED: Handling insufficient resources. 

                //if adding the upgrade task was successful, this would return true, if not then false.
                return addTaskMsg == TaskManager.AddTaskMsg.Success;
            }
            else
                return false;
        }
    }
}
