#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimatedText))]
public class AnimatedTextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty enumProp = serializedObject.FindProperty("textAnimationType");
        SerializedProperty triggerProp = serializedObject.FindProperty("triggerEffectWhenEnabled");

        //Enum
        DrawEnum(enumProp);
        
        EditorGUILayout.Space(8);

        //Trigger on enable
        EditorGUILayout.PropertyField(triggerProp);

        EditorGUILayout.Space(8);
        
        TextAnimationType type = (TextAnimationType)enumProp.enumValueIndex;

        EditorGUILayout.LabelField("Effect Parameters", EditorStyles.boldLabel);

        //Effect Parameters
        switch (type)
        {
            case TextAnimationType.Writing:
                CustomDraw.Draw("duration_Writing", serializedObject);
                break;

            case TextAnimationType.Floating:
                CustomDraw.Draw("amplitude_Floating", serializedObject);
                CustomDraw.Draw("speed_Floating", serializedObject);
                break;

            case TextAnimationType.Shaking:
                CustomDraw.Draw("loopShake", serializedObject);
                CustomDraw.Draw("intensity_Shake", serializedObject);
                CustomDraw.Draw("duration_Shake", serializedObject);
                break;

            case TextAnimationType.ColorPulse:
                CustomDraw.Draw("colorA_ColorPulse", serializedObject);
                CustomDraw.Draw("colorB_ColorPulse", serializedObject);
                CustomDraw.Draw("speed_ColorPulse", serializedObject);
                break;

            case TextAnimationType.RainbowWave:
                CustomDraw.Draw("speed_RainbowWave", serializedObject);
                CustomDraw.Draw("hueOffset_RainbowWave", serializedObject);
                break;

            case TextAnimationType.GradientReveal:
                CustomDraw.Draw("startColor_GradientReveal", serializedObject);
                CustomDraw.Draw("endColor_GradientReveal", serializedObject);
                CustomDraw.Draw("revealSpeed_GradientReveal", serializedObject);
                break;

            case TextAnimationType.ColorFlash:
                CustomDraw.Draw("flashColor_Flash", serializedObject);
                CustomDraw.Draw("duration_Flash", serializedObject);
                break;

            case TextAnimationType.ReadingHighlight:
                CustomDraw.Draw("baseColor_ReadingHighlight", serializedObject);
                CustomDraw.Draw("highlightColor_ReadingHighlight", serializedObject);
                CustomDraw.Draw("speed_ReadingHighlight", serializedObject);
                break;

            case TextAnimationType.MovingGradient:
                CustomDraw.Draw("loop_MovingGradient", serializedObject);
                CustomDraw.Draw("colorA_MovingGradient", serializedObject);
                CustomDraw.Draw("colorB_MovingGradient", serializedObject);
                CustomDraw.Draw("speed_MovingGradient", serializedObject);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawEnum(SerializedProperty enumProp)
    {
        TextAnimationType current = (TextAnimationType)enumProp.enumValueIndex;

        Color GetColor(TextAnimationType t)
        {
            return t switch
            {
                TextAnimationType.Writing => new Color(0.2f, 0.6f, 1f),
                TextAnimationType.Floating => new Color(0.2f, 1f, 0.6f),
                TextAnimationType.Shaking => new Color(1f, 0.4f, 0.4f),
                TextAnimationType.ColorPulse => new Color(1f, 0.6f, 0.2f),
                TextAnimationType.RainbowWave => new Color(0.8f, 0.2f, 1f),
                TextAnimationType.GradientReveal => new Color(0.4f, 0.4f, 1f),
                TextAnimationType.ColorFlash => new Color(1f, 1f, 0.2f),
                TextAnimationType.ReadingHighlight => new Color(0.2f, 1f, 1f),
                TextAnimationType.MovingGradient => new Color(0.6f, 0.6f, 0.6f),
                _ => Color.gray
            };
        }

        EditorGUILayout.LabelField("Text Animation Type", EditorStyles.boldLabel);

        float width = EditorGUIUtility.currentViewWidth;

        float buttonWidth = 110f;
        float buttonHeight = 28f;
        float spacing = 6f;

        int columns = Mathf.Max(1, Mathf.FloorToInt(width / (buttonWidth + spacing)));

        int count = 0;

        GUILayout.BeginVertical();

        foreach (TextAnimationType type in System.Enum.GetValues(typeof(TextAnimationType)))
        {
            if (count % columns == 0)
                GUILayout.BeginHorizontal();

            bool isSelected = type == current;

            Rect rect = GUILayoutUtility.GetRect(buttonWidth, buttonHeight);

            Color oldColor = GUI.backgroundColor;

            GUI.backgroundColor = GetColor(type);
            GUI.Box(rect, GUIContent.none, GUI.skin.button);

            GUI.backgroundColor = Color.white;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                normal = new GUIStyleState{textColor = Color.white}
            };

            GUI.Label(rect, type.ToString(), labelStyle);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                enumProp.enumValueIndex = (int)type;
                GUI.changed = true;
                Event.current.Use();
            }

            if (isSelected)
            {
                CustomDraw.DrawOutline(rect, Color.white, 2f);
            }

            GUI.backgroundColor = oldColor;

            count++;

            if (count % columns == 0)
                GUILayout.EndHorizontal();
        }

        if (count % columns != 0)
            GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

}

#endif