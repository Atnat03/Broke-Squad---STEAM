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
}

#endif
