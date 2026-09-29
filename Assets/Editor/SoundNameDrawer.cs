using Mono.Cecil;
using NUnit.Framework;
using ScriptableObjectsDefinitions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoundNameAttribute))]
public class SoundNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string[] keys = LoadKeys();
        string current = property.stringValue;
        int index = System.Array.IndexOf(keys, current);

        List<string> options = new List<string>(keys);

        bool isValid = index < 0;
        if(isValid)
        {
            string shown = string.IsNullOrEmpty(current) ? "(aucun)" : $"INVALIDE : {current.Replace('/', '>')}";
            options.Insert(0, $"Invalid: {shown}");
            index = 0;
        }

        EditorGUI.BeginChangeCheck();
        int picked = EditorGUI.Popup(position, label.text, index, options.ToArray());
        if(EditorGUI.EndChangeCheck())
        {
            if(isValid && picked == 0)
            {
                property.stringValue = "";
            }
            else
            {
                property.stringValue = options[picked];
            }
        }
    }

    private static string[] LoadKeys()
    {
        return AssetDatabase.FindAssets("t:SoundsDataSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<SoundsDataSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d => d != null)
            .SelectMany(d => d.sounds
                .Where(s => !string.IsNullOrEmpty(s.soundName))
                .Select(s => d.GetKey(s)))
            .ToArray();
    }
}
