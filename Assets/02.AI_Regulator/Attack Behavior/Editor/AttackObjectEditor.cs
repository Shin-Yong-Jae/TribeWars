﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using UnityEditor;

[CustomEditor(typeof(AttackObject))]
public class AttackObjectEditor : Editor
{
    SerializedObject attackObject_SO;

    public void OnEnable()
    {
        attackObject_SO = new SerializedObject((AttackObject)target);
    }

    public override void OnInspectorGUI()
    {
        attackObject_SO.Update();

        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("speed"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("mvtType"), new GUIContent("Movement Type"));
        if(attackObject_SO.FindProperty("mvtType").enumValueIndex == 1) //parabolic:
        {
            EditorGUILayout.PropertyField(attackObject_SO.FindProperty("maxHeight"));
            EditorGUILayout.PropertyField(attackObject_SO.FindProperty("minDistance"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("damageOnce"));
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("destroyOnDamage"));
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("childOnDamage"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("obstacleLayerMask"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("triggerEffect"));
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("hitEffect"));
        EditorGUILayout.PropertyField(attackObject_SO.FindProperty("hitAudio"));

        attackObject_SO.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
