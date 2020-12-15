using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{

    SerializedObject unit_SO;

    public void OnEnable()
    {
        unit_SO = new SerializedObject((Unit)target);
    }

    public override void OnInspectorGUI()
    {
        unit_SO.Update();

        EditorGUILayout.PropertyField(unit_SO.FindProperty("_name"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("code"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("category"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("description"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("icon"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("factionID"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("canBeConverted"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("free"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("populationSlots"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(unit_SO.FindProperty("coloredRenderers"), new GUIContent("Faction Colored Renderers"), true);
        EditorGUILayout.PropertyField(unit_SO.FindProperty("plane"), new GUIContent("Selection Plane"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("model"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("selection"), new GUIContent("Unit Selection"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("selectionAudio"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("animator"));
        EditorGUILayout.PropertyField(unit_SO.FindProperty("animatorOverrideController"));

        unit_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
        EditorUtility.SetDirty(target);
    }
}
