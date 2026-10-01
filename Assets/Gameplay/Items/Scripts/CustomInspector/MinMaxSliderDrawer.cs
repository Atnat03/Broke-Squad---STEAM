using UnityEngine;
using UnityEditor;

public class MinMaxSliderAttribute : PropertyAttribute
{
    public int Min { get; private set; }
    public int Max { get; private set; }

    public MinMaxSliderAttribute(int min, int max)
    {
        Min = min;
        Max = max;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
public class MinMaxSliderDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2
            + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        bool isVector2 =
            property.propertyType == SerializedPropertyType.Vector2;

        bool isVector2Int =
            property.propertyType == SerializedPropertyType.Vector2Int;

        if (!isVector2 && !isVector2Int)
        {
            EditorGUI.LabelField(
                position,
                label.text,
                "MinMaxSlider nécessite un Vector2 ou Vector2Int"
            );
            return;
        }

        MinMaxSliderAttribute range =
            (MinMaxSliderAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Première ligne : label
        Rect labelRect = new Rect(
            position.x,
            position.y,
            position.width,
            lineHeight
        );

        EditorGUI.LabelField(labelRect, label);

        // Deuxième ligne : champs min/max et slider
        Rect controlsRect = new Rect(
            position.x,
            position.y + lineHeight + spacing,
            position.width,
            lineHeight
        );

        float fieldWidth = 45f;
        float gap = 4f;

        Rect minRect = new Rect(
            controlsRect.x,
            controlsRect.y,
            fieldWidth,
            lineHeight
        );

        Rect maxRect = new Rect(
            controlsRect.xMax - fieldWidth,
            controlsRect.y,
            fieldWidth,
            lineHeight
        );

        Rect sliderRect = new Rect(
            minRect.xMax + gap,
            controlsRect.y,
            controlsRect.width - fieldWidth * 2 - gap * 2,
            lineHeight
        );

        float min;
        float max;

        if (isVector2Int)
        {
            Vector2Int value = property.vector2IntValue;
            min = value.x;
            max = value.y;
        }
        else
        {
            Vector2 value = property.vector2Value;
            min = value.x;
            max = value.y;
        }

        min = Mathf.Clamp(min, range.Min, range.Max);
        max = Mathf.Clamp(max, range.Min, range.Max);

        // Champs numériques
        if (isVector2Int)
        {
            min = EditorGUI.IntField(minRect, Mathf.RoundToInt(min));
            max = EditorGUI.IntField(maxRect, Mathf.RoundToInt(max));
        }
        else
        {
            min = EditorGUI.FloatField(minRect, min);
            max = EditorGUI.FloatField(maxRect, max);
        }

        min = Mathf.Clamp(min, range.Min, range.Max);
        max = Mathf.Clamp(max, range.Min, range.Max);

        // Garantit min <= max
        if (min > max)
        {
            min = max;
        }

        // Slider à deux curseurs
        EditorGUI.MinMaxSlider(
            sliderRect,
            ref min,
            ref max,
            range.Min,
            range.Max
        );

        // Enregistre les valeurs dans le bon type
        if (isVector2Int)
        {
            property.vector2IntValue = new Vector2Int(
                Mathf.RoundToInt(min),
                Mathf.RoundToInt(max)
            );
        }
        else
        {
            property.vector2Value = new Vector2(min, max);
        }

        EditorGUI.EndProperty();
    }
}
#endif