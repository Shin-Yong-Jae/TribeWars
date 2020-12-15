using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(UnitMovement))]
public class UnitMovementEditor : Editor
{
    SerializedObject movement_SO;

    public void OnEnable()
    {
        movement_SO = new SerializedObject((UnitMovement)target);
    }

    public override void OnInspectorGUI()
    {
        movement_SO.Update();

        EditorGUILayout.PropertyField(movement_SO.FindProperty("canMove"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("canFly"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("speed"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("acceleration"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("targetPositionCollider"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(movement_SO.FindProperty("canMoveRotate"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("minMoveAngle"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("canIdleRotate"));
        if(movement_SO.FindProperty("canIdleRotate").boolValue == true)
        {
            EditorGUILayout.PropertyField(movement_SO.FindProperty("rotationDamping"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(movement_SO.FindProperty("mvtOrderAudio"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("mvtAudio"));
        EditorGUILayout.PropertyField(movement_SO.FindProperty("invalidMvtPathAudio"));

        movement_SO.ApplyModifiedProperties(); //apply all modified properties always at the end of this method.
        EditorUtility.SetDirty(target);
    }
}
