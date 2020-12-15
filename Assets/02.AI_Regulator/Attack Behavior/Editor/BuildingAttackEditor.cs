using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using UnityEditor;

[CustomEditor(typeof(BuildingAttack))]
public class BuildingAttackEditor : Editor
{
    SerializedObject attack_SO;
    int tabID = 0;

    public void OnEnable()
    {
        attack_SO = new SerializedObject((BuildingAttack)target);
    }

    public override void OnInspectorGUI()
    {
        attack_SO.Update();

        EditorGUI.BeginChangeCheck();

        tabID = GUILayout.Toolbar(tabID, new string[] { "General", "Damage", "Weapon", "LOS" });

        if (EditorGUI.EndChangeCheck())
        {
            attack_SO.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        switch (tabID)
        {
            case 0:
                UnitAttackEditor.OnAttackEntityInspectorGUI(attack_SO, false);
                break;
            case 1:
                UnitAttackEditor.OnAttackDamageInspectorGUI(attack_SO);
                break;
            case 2:
                UnitAttackEditor.OnAttackWeaponInspectorGUI(attack_SO);
                break;
            case 3:
                UnitAttackEditor.OnAttackLOSInspectorGUI(attack_SO);
                break;
            case 4:
                UnitAttackEditor.OnAttackEventsInspectorGUI(attack_SO);
                break;
            default:
                break;
        }

        attack_SO.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
