using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Game Manager Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor {

	int FactionID; //current faction that the user is configuring.
    GameManager gameManager;
    SerializedObject gameManager_SO;

    public void OnEnable()
    {
        gameManager = (GameManager)target;
        gameManager_SO = new SerializedObject(gameManager);
    }

    public override void OnInspectorGUI ()
	{
		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Factions:", TitleGUIStyle);
		EditorGUILayout.Space ();

		if (GUILayout.Button ("Add Faction (Faction Count: " + gameManager.Factions.Count + ")")) {
			GameManager.FactionInfo NewFaction = new GameManager.FactionInfo ();
            gameManager.Factions.Add (NewFaction);

			FactionID = gameManager.Factions.Count - 1;
		}


		EditorGUILayout.Space ();
		EditorGUILayout.HelpBox ("Make sure to create the maximum amount that this map can handle. When fewer factions play on the map, the rest will be automatically removed.", MessageType.Info);

		EditorGUILayout.Space ();
		if (GUILayout.Button (">>")) {
			ChangeFactionID (1, gameManager.Factions.Count);
		}
		if (GUILayout.Button ("<<")) {
			ChangeFactionID (-1, gameManager.Factions.Count);
		}

		EditorGUILayout.Space ();
		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("Faction ID " + FactionID.ToString (), TitleGUIStyle);
		EditorGUILayout.Space ();


        gameManager.Factions [FactionID].Name = EditorGUILayout.TextField ("Faction Name", gameManager.Factions [FactionID].Name);
        gameManager.Factions[FactionID].TypeInfo = EditorGUILayout.ObjectField("Faction Type", gameManager.Factions[FactionID].TypeInfo, typeof(FactionTypeInfo), false) as FactionTypeInfo;

        gameManager.Factions [FactionID].FactionColor = EditorGUILayout.ColorField ("Faction Color", gameManager.Factions [FactionID].FactionColor);

        gameManager.Factions [FactionID].playerControlled = EditorGUILayout.Toggle ("Player Controlled", gameManager.Factions [FactionID].playerControlled);
		EditorGUILayout.HelpBox ("Make sure that only one team is controlled the player.", MessageType.Info);

        gameManager.Factions [FactionID].maxPopulation = EditorGUILayout.IntField ("Initial Maximum Population", gameManager.Factions [FactionID].maxPopulation);
        gameManager.Factions [FactionID].CapitalBuilding = EditorGUILayout.ObjectField ("Capital Building", gameManager.Factions [FactionID].CapitalBuilding, typeof(Building), true) as Building;
        gameManager.Factions[FactionID].npcMgr = EditorGUILayout.ObjectField("NPC Manager", gameManager.Factions[FactionID].npcMgr, typeof(NPCManager), true) as NPCManager;

        EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		if (GUILayout.Button ("Remove Faction")) {
			if (gameManager.Factions.Count > 2) {
                gameManager.Factions.RemoveAt(FactionID);
                ChangeFactionID(-1, gameManager.Factions.Count);
            } else {
				Debug.LogError ("The minimum amount of factions to have in one map is: 2!");
			}
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space (); 
		TitleGUIStyle.fontSize = 20;
		EditorGUILayout.LabelField ("General Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();

        EditorGUILayout.PropertyField(gameManager_SO.FindProperty("defeatCondition"));
        EditorGUILayout.PropertyField(gameManager_SO.FindProperty("speedModifier"));
        EditorGUILayout.PropertyField(gameManager_SO.FindProperty("randomFactionSlots"));
		gameManager.MainMenuScene = EditorGUILayout.TextField ("Main Menu Scene", gameManager.MainMenuScene);
		gameManager.PeaceTime = EditorGUILayout.FloatField ("Peace Time (seconds)", gameManager.PeaceTime);
		EditorGUILayout.LabelField ("General Audio Source");
		gameManager.GeneralAudioSource = EditorGUILayout.ObjectField (gameManager.GeneralAudioSource, typeof(AudioSource), true) as AudioSource;
        EditorGUILayout.PropertyField(gameManager_SO.FindProperty("winGameAudio"));
        EditorGUILayout.PropertyField(gameManager_SO.FindProperty("loseGameAudio"));

        gameManager_SO.ApplyModifiedProperties();
		EditorUtility.SetDirty (gameManager);
	}

	public void ChangeFactionID (int Value, int Max)
	{
		int ProjectedID = FactionID + Value;
		if (ProjectedID < Max && ProjectedID >= 0) {
			FactionID = ProjectedID;
		}
	}
}
