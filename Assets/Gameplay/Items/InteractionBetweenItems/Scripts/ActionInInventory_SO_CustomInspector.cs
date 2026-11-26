#if UNITY_EDITOR

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Gameplay.Items;

[CustomEditor(typeof(ActionInInventory_SO))]
public class ActionInInventory_SO_CustomInspector : Editor
{
    private List<Type> actionTypes;
    private string[] actionNames;

    private void OnEnable()
    {
        RefreshActionTypes();
    }

    private void RefreshActionTypes()
    {
        actionTypes = TypeCache.GetTypesDerivedFrom<IActionInInventory>()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                !type.ContainsGenericParameters)
            .OrderBy(type => type.Name)
            .ToList();

        actionNames = actionTypes
            .Select(type => type.Name)
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Items
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemToDoFirstAction"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemToConsumeCondition"));

        EditorGUILayout.Space(10);

        // Action dropdown
        SerializedProperty actionProperty = serializedObject.FindProperty("ActionInInventory");

        DrawActionDropdown(actionProperty);
        
        EditorGUILayout.Space(10);
        
        //Tooltip
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Tooltip"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Icon"));
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawActionDropdown(SerializedProperty property)
    {
        IActionInInventory currentAction = property.managedReferenceValue as IActionInInventory;

        int currentIndex = currentAction != null ? actionTypes.IndexOf(currentAction.GetType()) : -1;
        
        int selectedIndex = EditorGUILayout.Popup("Action In Inventory", currentIndex, actionNames);

        if (selectedIndex != currentIndex && selectedIndex >= 0)
        {
            Type selectedType = actionTypes[selectedIndex];

            property.managedReferenceValue = Activator.CreateInstance(selectedType);

            serializedObject.ApplyModifiedProperties();
        }

        if (property.managedReferenceValue != null)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Action Parameters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property, true);
        }
    }
}

#endif