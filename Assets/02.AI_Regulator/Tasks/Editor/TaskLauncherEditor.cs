using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Task Launcher Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(TaskLauncher))]
public class TaskLauncherEditor : Editor
{
    TaskLauncher Target;
    SerializedObject SOTarget;
    TaskLauncher.TasksVars CurrentTask;
    string TaskPath;
    string TabName;

    //General serialized properties (applicable to all task types):
    SerializedProperty TaskResources;
    SerializedProperty tasksToUnlock;

    //Unit creation task type only serialized properties:
    SerializedProperty CreateUnit_Prefabs;

    //Properties for the task events:
    SerializedProperty Event_Launch;
    SerializedProperty Event_Complete;
    SerializedProperty Event_Cancel;

    GUIStyle TitleGUIStyle = new GUIStyle();

    public void OnEnable ()
    {
        Target = (TaskLauncher)target;

        SOTarget = new SerializedObject(Target);
    }

    void TaskLauncherSettings ()
    {
        //Task Launcher settings:
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Task Launcher Settings:", TitleGUIStyle);
        EditorGUILayout.Space();

        Target.Code = EditorGUILayout.TextField("Task Launcher Code:", Target.Code);
        Target.MinTaskHealth = EditorGUILayout.FloatField("Min Health (to launch task):", Target.MinTaskHealth);
        Target.MaxTasks = EditorGUILayout.IntField("Max Allowed Active Tasks:", Target.MaxTasks);
        Target.LaunchTaskAudio = EditorGUILayout.ObjectField ("Launch Task Sound Effect:", Target.LaunchTaskAudio, typeof(AudioClip), true) as AudioClip;
        Target.DeclinedTaskAudio = EditorGUILayout.ObjectField ("Declined Task Sound Effect:", Target.DeclinedTaskAudio, typeof(AudioClip), true) as AudioClip;
    }

    void GeneralTaskSettings ()
    {
        CurrentTask.Code = EditorGUILayout.TextField("Code:", CurrentTask.Code);

        CurrentTask.IsAvailable = EditorGUILayout.Toggle("Available by default?", CurrentTask.IsAvailable);

        CurrentTask.FactionSpecific = EditorGUILayout.Toggle("Faction Specific?", CurrentTask.FactionSpecific);
        if (CurrentTask.FactionSpecific == true)
        {
            CurrentTask.FactionCode = EditorGUILayout.TextField("Faction Code:", CurrentTask.FactionCode);
        }

        CurrentTask.AllowedTaskType = (TaskLauncher.AllowedTaskTypes)EditorGUILayout.EnumPopup("Task Type:", CurrentTask.AllowedTaskType);
        switch (CurrentTask.AllowedTaskType)
        {
            case TaskLauncher.AllowedTaskTypes.CreateUnit:
                CurrentTask.TaskType = TaskManager.TaskTypes.CreateUnit;
                break;
            case TaskLauncher.AllowedTaskTypes.Destroy:
                CurrentTask.TaskType = TaskManager.TaskTypes.Destroy;
                break;
            case TaskLauncher.AllowedTaskTypes.unitUpgrade:
                CurrentTask.TaskType = TaskManager.TaskTypes.unitUpgrade;
                break;
            case TaskLauncher.AllowedTaskTypes.buildingUpgrade:
                CurrentTask.TaskType = TaskManager.TaskTypes.buildingUpgrade;
                break;
            case TaskLauncher.AllowedTaskTypes.CustomTask:
                CurrentTask.TaskType = TaskManager.TaskTypes.CustomTask;
                break;
            default:
                break;
        }

        CurrentTask.Description = EditorGUILayout.TextField("Description:", CurrentTask.Description);
        CurrentTask.TaskPanelCategory = EditorGUILayout.IntField("Task Panel Category:", CurrentTask.TaskPanelCategory);
        CurrentTask.TaskIcon = EditorGUILayout.ObjectField("Icon:", CurrentTask.TaskIcon, typeof(Sprite), true) as Sprite;
        CurrentTask.ReloadTime = EditorGUILayout.FloatField("Reload Time:", CurrentTask.ReloadTime);

        EditorGUILayout.Space();
        TaskResources = SOTarget.FindProperty(TaskPath).FindPropertyRelative("RequiredResources");
        if(TaskResources != null)
            EditorGUILayout.PropertyField(TaskResources, true);

        EditorGUILayout.Space();
        CurrentTask.UseOnce = EditorGUILayout.Toggle("One-time use?", CurrentTask.UseOnce);
        if(CurrentTask.UseOnce == true)
            CurrentTask.useOnceOnAllInstances = EditorGUILayout.Toggle("Use once on all instances?", CurrentTask.useOnceOnAllInstances);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Input codes of tasks that you want to enable when this task is completed.", MessageType.Info);
        EditorGUILayout.Space();

        tasksToUnlock = SOTarget.FindProperty(TaskPath).FindPropertyRelative("tasksToUnlock");
        if (tasksToUnlock != null)
            EditorGUILayout.PropertyField(tasksToUnlock, true);
        EditorGUILayout.Space();

        CurrentTask.TaskCompletedAudio = EditorGUILayout.ObjectField("On Complete Audio:", CurrentTask.TaskCompletedAudio, typeof(AudioClip), true) as AudioClip;
    }

    void CreateUnitSettings ()
    {
        EditorGUILayout.LabelField("Unit Creation Settings:", TitleGUIStyle);
        EditorGUILayout.HelpBox("The prefabs list allows multiple unit prefabs to be added but only one (randomly selected) will be created each time.", MessageType.Warning);
        EditorGUILayout.Space();

        CreateUnit_Prefabs = SOTarget.FindProperty(TaskPath).FindPropertyRelative("UnitCreationSettings.Prefabs");
        EditorGUILayout.PropertyField(CreateUnit_Prefabs, true);
    }

    //Upgrade (Unit+Building) settings:
    void UpgradeSettings (TaskManager.TaskTypes upgradeType)
    {
        switch(upgradeType)
        {
            case TaskManager.TaskTypes.unitUpgrade: //unit upgrade:
                
                //show the unit upgrade field:
                CurrentTask.unitUpgrade = EditorGUILayout.ObjectField("Unit Prefab", CurrentTask.unitUpgrade, typeof(UnitUpgrade), false) as UnitUpgrade;
                
                break;

            case TaskManager.TaskTypes.buildingUpgrade: //building upgrade:

                //show the unit upgrade field:
                CurrentTask.buildingUpgrade = EditorGUILayout.ObjectField("Unit Prefab", CurrentTask.buildingUpgrade, typeof(BuildingUpgrade), false) as BuildingUpgrade;

                break;

            default:
                break;
        }

        EditorGUILayout.PropertyField(SOTarget.FindProperty(TaskPath + ".upgradeTargetID"));

        EditorGUILayout.HelpBox("Upgrade settings can be only modified from the prefab directly. You can only pick the ID of the upgrade target here.", MessageType.Warning);
    }

    void EventsSettings ()
    {
        EditorGUILayout.HelpBox("In addition to the delegate events (which are called for all tasks), you can trigger events for this task independently.", MessageType.Info);
        EditorGUILayout.Space();

        Event_Launch = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskLaunchEvent");
        EditorGUILayout.PropertyField(Event_Launch, true);

        Event_Complete = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskCompleteEvent");
        EditorGUILayout.PropertyField(Event_Complete, true);

        Event_Cancel = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskCancelEvent");
        EditorGUILayout.PropertyField(Event_Cancel, true);
    }

    void TaskSettings ()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Task Settings:", TitleGUIStyle);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Go through tasks, add/remove tasks using the buttons below.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Add Task (Task Count: " + Target.TasksList.Count + ")"))
        {
            TaskLauncher.TasksVars NewTask = new TaskLauncher.TasksVars();
            Target.TasksList.Add(NewTask);

            Target.TaskID = Target.TasksList.Count - 1;
        }

        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("<<"))
            {
                Move(ref Target.TaskID, -1, Target.TasksList.Count);
            }
            if (GUILayout.Button(">>"))
            {
                Move(ref Target.TaskID, 1, Target.TasksList.Count);
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //making sure there tasks to begin with:
        if (Target.TasksList.Count > 0)
        {
            //Tasks:
            //task to display:
            CurrentTask = Target.TasksList[Target.TaskID];
            TaskPath = "TasksList.Array.data[" + Target.TaskID.ToString() + "]";

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Task ID: " + Target.TaskID.ToString(), TitleGUIStyle);
            EditorGUILayout.Space();

            TitleGUIStyle.alignment = TextAnchor.MiddleLeft;
            TitleGUIStyle.fontStyle = FontStyle.Bold;

            EditorGUI.BeginChangeCheck();

            Target.TabID = GUILayout.Toolbar(Target.TabID, new string[] { "General Settings", "Custom Settings", "Events" });

            switch (Target.TabID)
            {
                case 0:
                    TabName = "General Settings";
                    break;
                case 1:
                    //The following settings will be shown depending on the task's type:
                    TabName = "Custom Settings";
                    break;

                case 2:
                    TabName = "Events";
                    break;
                default:
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SOTarget.ApplyModifiedProperties();
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space();

            switch (TabName)
            {
                case "General Settings":
                    GeneralTaskSettings();
                    break;
                case "Custom Settings":
                    //The following settings will be shown depending on the task's type:
                    switch (CurrentTask.TaskType)
                    {
                        case TaskManager.TaskTypes.CreateUnit: //Unit creation task:

                            CreateUnitSettings();

                            break;

                        case TaskManager.TaskTypes.unitUpgrade: //Unit Upgrade task

                            UpgradeSettings(TaskManager.TaskTypes.unitUpgrade);

                            break;

                        case TaskManager.TaskTypes.buildingUpgrade: //Unit Upgrade task

                            UpgradeSettings(TaskManager.TaskTypes.buildingUpgrade);

                            break;

                        default:
                            EditorGUILayout.HelpBox("No custom settings for the current task type.", MessageType.None);

                            break;
                    }
                    break;

                case "Events":
                    EventsSettings();

                    break;
                default:
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Task"))
            {
                Target.TasksList.RemoveAt(Target.TaskID);
                if (Target.TaskID > 0)
                    Move(ref Target.TaskID, -1, Target.TasksList.Count);
            }
        }

        else
        {
            EditorGUILayout.HelpBox("There are no tasks, create one using the button above.", MessageType.Warning);
        }
    }

    public override void OnInspectorGUI()
    {
        SOTarget.Update(); //Always update the Serialized Object.

        TitleGUIStyle.fontSize = 13;
        TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
        TitleGUIStyle.fontStyle = FontStyle.Bold;

        TaskLauncherSettings();

        TaskSettings();

        SOTarget.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        EditorUtility.SetDirty(target);
    }

    void Move (ref int ID, int Step, int Max)
    {
        if (ID + Step >= 0 && ID + Step < Max)
        {
            ID += Step;
        }
    }
}
