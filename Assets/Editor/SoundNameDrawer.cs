using ScriptableObjectsDefinitions;
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

        string[] soundNames = LoadSoundsNames();
        int index = System.Array.IndexOf(soundNames, property.stringValue);

        if (index < 0)
        {
            soundNames = soundNames.Prepend($"<invalid : {property.stringValue}>").ToArray();
            index = 0;
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, index, soundNames);
            if (EditorGUI.EndChangeCheck() && picked > 0)
                property.stringValue = soundNames[picked];
            return;
        }

        EditorGUI.BeginChangeCheck();
        index = EditorGUI.Popup(position, label.text, index, soundNames);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = soundNames[index];
        }
    }

    private static string[] LoadSoundsNames()
    {
        string[] guids = AssetDatabase.FindAssets("t:SoundsDataSO");

        if (guids.Length == 0) return new string[0];

        var data = AssetDatabase.LoadAssetAtPath<SoundsDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return data.sounds
            .Select(s => s.soundName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();
    }
}
