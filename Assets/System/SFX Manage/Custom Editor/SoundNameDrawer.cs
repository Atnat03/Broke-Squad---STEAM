#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using ScriptableObjectsDefinitions;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoundNameAttribute))]
public class SoundNameDrawer : PropertyDrawer
{
    private static string[] cachedKeys = new string[0];
    private static double lastCacheTime = -10;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string[] keys = LoadKeys();
        string current = property.stringValue;
        int index = System.Array.IndexOf(keys, current);

        List<string> options = new List<string>(keys);

        bool isInvalid = index < 0;
        if (isInvalid)
        {
            string shown = string.IsNullOrEmpty(current)
                ? "(aucun)"
                : $"INVALIDE : {current.Replace('/', '>')}";
            options.Insert(0, shown);
            index = 0;
        }

        EditorGUI.BeginChangeCheck();
        int picked = EditorGUI.Popup(position, label.text, index, options.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (isInvalid && picked == 0) return;
            property.stringValue = options[picked];
        }
    }

    private static string[] LoadKeys()
    {
        if (EditorApplication.timeSinceStartup - lastCacheTime < 1.0)
            return cachedKeys;

        lastCacheTime = EditorApplication.timeSinceStartup;

        cachedKeys = AssetDatabase.FindAssets("t:SoundsDataSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<SoundsDataSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d => d != null)
            .SelectMany(d => d.sounds
                .Where(s => !string.IsNullOrEmpty(s.soundName))
                .Select(s => d.GetKey(s)))
            .ToArray();

        return cachedKeys;
    }
}

#endif