#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public sealed class CustomDraw : Editor 
{
    public static void Draw(string propertyName, SerializedObject s)
    {
        SerializedProperty prop = s.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop);
            EditorGUILayout.Space(2);
        }
        else
        {
            EditorGUILayout.HelpBox($"Missing field: {propertyName}", MessageType.Warning);
        }
    }
    
    public static void DrawOutline(Rect rect, Color color, float thickness)
    {
        Color old = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
    
    public static void DrawEnumDropdown(SerializedProperty enumProperty, string label = null)
    {
        if (enumProperty == null || enumProperty.propertyType != SerializedPropertyType.Enum)
        {
            Debug.LogWarning("DrawEnumDropdown: la propriété n'est pas un enum.");
            return;
        }

        GUIContent content = label != null ? new GUIContent(label) : new GUIContent(enumProperty.displayName);

        int selectedIndex = EditorGUILayout.Popup(content, enumProperty.enumValueIndex, enumProperty.enumDisplayNames);

        if (selectedIndex != enumProperty.enumValueIndex)
        {
            enumProperty.enumValueIndex = selectedIndex;
        }
    }
}

#endif
