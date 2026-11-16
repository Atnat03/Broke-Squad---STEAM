#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [CustomEditor(typeof(DragHandle))]
    public class DragHandleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "axis");
            
            SerializedProperty axisProperty = serializedObject.FindProperty("axis");

            DragHandle handle = (DragHandle)target;
            HeavyDraggable owner = handle.GetComponentInParent<HeavyDraggable>();

            if (owner != null)
            {
                SerializedObject ownerSO = new SerializedObject(owner);
                ownerSO.Update();

                SerializedProperty axesProperty = ownerSO.FindProperty("axes");

                if (axesProperty != null && axesProperty.arraySize > 0)
                {
                    string[] options = new string[axesProperty.arraySize];

                    for (int i = 0; i < options.Length; i++)
                    {
                        SerializedProperty element = axesProperty.GetArrayElementAtIndex(i);

                        options[i] = element.FindPropertyRelative("name").stringValue;
                    }

                    axisProperty.intValue = Mathf.Clamp(axisProperty.intValue, 0, options.Length - 1);

                    axisProperty.intValue = EditorGUILayout.Popup("Axis", axisProperty.intValue, options);
                }
                else
                {
                    EditorGUILayout.PropertyField(axisProperty);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(axisProperty);
            }
            serializedObject.ApplyModifiedProperties();
            
            
            
        }
        
        
    }
}
#endif