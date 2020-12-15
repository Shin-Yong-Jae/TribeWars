using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/* Unit Task UI script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class TaskUI : MonoBehaviour {

		[HideInInspector]
		public int ID = -1;

		public TaskManager.TaskTypes TaskType; //the task's type goes here.

        //For multiple selection icons, we use the same script:
        public Image EmptyBar;
        public Image FullBar;

        [HideInInspector]
        public TaskLauncher TaskComp; //The task launcher connected to this task, if there's one.

		[HideInInspector]
		public Sprite TaskSprite; //for component tasks, they need their sprites them in order to change the cursor properly if the settings allow

		UIManager UIMgr;
		SelectionManager SelectionMgr;
        TaskManager TaskMgr;

		void Start () {
			UIMgr = GameManager.Instance.UIMgr;
			SelectionMgr = GameManager.Instance.SelectionMgr;
            TaskMgr = GameManager.Instance.TaskMgr;
		}

        //method to launch the task.
		public void OnTaskClick ()
		{
            if(TaskComp != null) //if this task is connected to task component
            {
                //add it to the task launcher queue
                TaskMgr.AddTask(TaskComp, ID, TaskType);
            }
            else
            {
                //launch native task here:
                TaskMgr.AddTask(ID, TaskType, TaskSprite);
            }
		}

        public void ShowTaskInfo()
        {
            //This method is called whenever the mouse hovers over a task button:
            //First activate both the task info menu and text objects to show them for the player:
            string Msg = "";

            ResourceManager.Resources[] RequiredResources = new ResourceManager.Resources[0];

            if (TaskComp != null) //if there's a task launcher connected to this task
            {
                RequiredResources = TaskComp.TasksList[ID].RequiredResources;
                Msg = TaskComp.TasksList[ID].Description;
            }

            //Non-Task Launcher tasks:

            //If the player is currently selecting building:
            else if (SelectionMgr.SelectedBuilding != null)
            {
                //We'll show the task info for this building.

                if (TaskType != TaskManager.TaskTypes.ResourceGen)
                { //if it's not a resource collection task:

                    if (TaskType == TaskManager.TaskTypes.APCEject)
                        Msg = "Eject unit: " + SelectionMgr.SelectedBuilding.gameObject.GetComponent<APC>().GetStoredUnit(ID).GetName() + " from the APC.";
                    else if (TaskType == TaskManager.TaskTypes.APCEjectAll)
                        Msg = "Eject all units inside the APC.";
                    else if (TaskType == TaskManager.TaskTypes.APCCall)
                        Msg = "Call units to get in the APC";
                }
                else
                {
                    ResourceGenerator.Generator g = SelectionMgr.SelectedBuilding.GeneratorComp.GetGenerator(ID);
                    Msg = "Maximum amount reached! Click to collect " + g.GetCurrAmount().ToString() + " of " + g.GetResourceName();
                }

            }
            else if(SelectionMgr.SelectedUnits.Count > 0)
            {
                //If the player is currently selecting units, we'll display a description of the tasks they can do:

                //If it's a task that allows the player to place a building to construct it:
                if (TaskType == TaskManager.TaskTypes.PlaceBuilding)
                {
                    //Get the building associated with this task ID:
                    Building CurrentBuilding = UIMgr.PlacementMgr.AllBuildings[ID].GetComponent<Building>();

                    Msg += CurrentBuilding.GetName() + ": " + CurrentBuilding.GetDescription();

                    RequiredResources = CurrentBuilding.GetResources();

                    if (CurrentBuilding.GetRequiredBuildings().Count > 0)
                    {
                        Msg += "\n<b>Required Buildings:</b>";
                        //Go through all the required buildings to place this one:
                        for (int i = 0; i < CurrentBuilding.GetRequiredBuildings().Count; i++)
                        {
                            if (i > 0)
                            {
                                Msg += " -";
                            }
                            Msg += " " + CurrentBuilding.GetRequiredBuildings()[i].GetName();
                        }
                    }
                }
                if (TaskType == TaskManager.TaskTypes.APCEject)
                    Msg = "Eject unit: " + GameManager.Instance.SelectionMgr.SelectedUnits[0].APCComp.GetStoredUnit(ID).GetName() + " from the APC.";
                else if (TaskType == TaskManager.TaskTypes.APCEjectAll)
                    Msg = "Eject all units inside the APC.";
                else if (TaskType == TaskManager.TaskTypes.APCCall)
                    Msg = "Call units to get in the APC";
                else if (TaskType == TaskManager.TaskTypes.Mvt)
                {
                    Msg = "Move unit(s).";
                }
                else if (TaskType == TaskManager.TaskTypes.Build)
                {
                    Msg = "Construct a building.";
                }
                else if (TaskType == TaskManager.TaskTypes.Collect)
                {
                    Msg = "Collect resources.";
                }
                else if (TaskType == TaskManager.TaskTypes.Attack)
                {
                    Msg = "Attack enemy units/buildings.";
                }
                else if (TaskType == TaskManager.TaskTypes.Heal)
                {
                    Msg = "Heal unit(s).";
                }
                else if (TaskType == TaskManager.TaskTypes.Convert)
                {
                    Msg = "Convert enemy unit(s).";
                }
                else if (TaskType == TaskManager.TaskTypes.AttackTypeSelection)
                {
                    Msg = "Switch attack type.";
                }
                else if(TaskType == TaskManager.TaskTypes.ToggleWander)
                {
                    Msg = "Toggle wandering.";
                }

            }

            //Then we will go through all the required resources..
            if (RequiredResources.Length > 0)
            {
                //And show them:
                Msg += "\n<b>Required Resources :</b>";

                for (int i = 0; i < RequiredResources.Length; i++)
                {
                    if (i > 0)
                    {
                        Msg += " -";
                    }
                    Msg += " " + RequiredResources[i].Name + ": " + RequiredResources[i].Amount.ToString();
                }
            }

            //show the task info on the tooltip:
            UIMgr.ShowTooltip(Msg);
        }
	}
}