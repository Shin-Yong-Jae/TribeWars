using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(ResourceCollector))]
public class ResourceCollectorEditor : Editor
{
    SerializedObject collector_SO;

    public void OnEnable()
    {
        collector_SO = new SerializedObject((ResourceCollector)target);
    }

    public override void OnInspectorGUI()
    {
        collector_SO.Update();

        EditorGUILayout.PropertyField(collector_SO.FindProperty("collectionObjects"), new GUIContent("Collection Settings"), true);
        EditorGUILayout.PropertyField(collector_SO.FindProperty("dropOffObject"));
        EditorGUILayout.PropertyField(collector_SO.FindProperty("dropOffOverrideController"));
        EditorGUILayout.PropertyField(collector_SO.FindProperty("maxCapacity"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(collector_SO.FindProperty("autoBehavior"), new GUIContent("Auto Collect"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(collector_SO.FindProperty("sourceEffect"));
        EditorGUILayout.PropertyField(collector_SO.FindProperty("targetEffect"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(collector_SO.FindProperty("orderAudio"), new GUIContent("Collection Order Audio"));

        collector_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
        EditorUtility.SetDirty(target);
    }
}
