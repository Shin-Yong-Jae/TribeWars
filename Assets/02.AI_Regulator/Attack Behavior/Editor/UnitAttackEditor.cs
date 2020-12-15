using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;
using UnityEditor;

[CustomEditor(typeof(UnitAttack))]
public class UnitAttackEditor : Editor
{

    SerializedObject attack_SO;
    int tabID = 0;

    public void OnEnable()
    {
        attack_SO = new SerializedObject((UnitAttack)target);
    }

    public override void OnInspectorGUI()
    {
        attack_SO.Update();

        EditorGUI.BeginChangeCheck();

        tabID = GUILayout.Toolbar(tabID, new string[] { "General", "Damage", "Weapon", "LOS", "Events" });

        if (EditorGUI.EndChangeCheck())
        {
            attack_SO.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        switch (tabID)
        {
            case 0:
                OnAttackEntityInspectorGUI(attack_SO, true);
                break;
            case 1:
                OnAttackDamageInspectorGUI(attack_SO);
                break;
            case 2:
                OnAttackWeaponInspectorGUI(attack_SO);
                break;
            case 3:
                OnAttackLOSInspectorGUI(attack_SO);
                break;
            case 4:
                OnAttackEventsInspectorGUI(attack_SO);
                break;
            default:
                break;
        }

        attack_SO.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    public static void OnAttackEntityInspectorGUI(SerializedObject SO, bool unit)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
        EditorGUILayout.PropertyField(SO.FindProperty("code"));
        EditorGUILayout.PropertyField(SO.FindProperty("icon"));
        EditorGUILayout.PropertyField(SO.FindProperty("basic"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackPower"));
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("rangeTypeCode"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("engageAllTypes"));
        if (SO.FindProperty("engageAllTypes").boolValue == false)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("engageUnits"));
            EditorGUILayout.PropertyField(SO.FindProperty("engageBuildings"));
            EditorGUILayout.PropertyField(SO.FindProperty("engageInList"));
            EditorGUILayout.PropertyField(SO.FindProperty("codesList"), true);
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("engageOnAssign"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageWhenAttacked"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageOnce"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageFriendly"));
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("moveOnAttack"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("engageInRange"));
        if (SO.FindProperty("engageInRange").boolValue == true)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("searchRange"));
            EditorGUILayout.PropertyField(SO.FindProperty("searchReload"));
        }
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("followDistance"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("direct"));
        if (SO.FindProperty("direct").boolValue == false)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackObjectLauncher.launchType"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackObjectLauncher.sources"), true);
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("delayDuration"));
        EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("triggerAnimationInDelay"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("useReload"));
        EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("coolDownEnabled"));
        EditorGUILayout.PropertyField(SO.FindProperty("coolDownDuration"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("reloadDealtDamage"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("orderAudio"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackAudio"));
    }

    public static void OnAttackDamageInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.canDealDamage"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.unitDamage"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.buildingDamage"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.customDamages"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.areaAttackEnabled"));
        if (SO.FindProperty("damage.areaAttackEnabled").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("damage.attackRanges"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.dotEnabled"));
        if (SO.FindProperty("damage.dotEnabled").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("damage.dotAttributes"), new GUIContent("DoT Attributes"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.effect"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.effectLifeTime"));
    }

    public static void OnAttackWeaponInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.weaponObject"));
        if (SO.FindProperty("weapon.weaponObject").objectReferenceValue == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.smoothRotation"));
        if (SO.FindProperty("weapon.smoothRotation").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.rotationDamping"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationX"));
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationY"));
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationZ"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.forceIdleRotation"));
        if (SO.FindProperty("weapon.forceIdleRotation").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.idleAngles"));
    }

    public static void OnAttackLOSInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.enable"));
        if (SO.FindProperty("lineOfSight.enable").boolValue == false)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.useWeaponObject"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.LOSAngle"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationX"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationY"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationZ"));
    }

    public static void OnAttackEventsInspectorGUI (SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("attackerInRangeEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("targetLockedEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackPerformedEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackDamageDealtEvent"));
    }
}
