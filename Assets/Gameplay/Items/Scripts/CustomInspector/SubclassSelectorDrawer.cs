#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Utiliser avec [SerializeReference]");
                return;
            }

            Type currentType = property.managedReferenceValue?.GetType();
            string buttonLabel = currentType == null
                ? "None"
                : ObjectNames.NicifyVariableName(currentType.Name);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);

            if (EditorGUI.DropdownButton(fieldRect, new GUIContent(buttonLabel), FocusType.Keyboard))
                ShowMenu(fieldRect, property, fieldInfo.FieldType, currentType);
        }

        private static void ShowMenu(Rect rect, SerializedProperty property, Type baseType, Type currentType)
        {
            // "property" ne sera plus valide au moment du clic : on garde le SerializedObject et le chemin
            SerializedObject so = property.serializedObject;
            string path = property.propertyPath;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), currentType == null, () => Assign(so, path, null));

            foreach (Type type in SerializeReferenceListDrawer.GetImplementations(baseType))
            {
                Type captured = type;
                menu.AddItem(
                    new GUIContent(ObjectNames.NicifyVariableName(type.Name)),
                    type == currentType,
                    () => Assign(so, path, captured));
            }

            menu.DropDown(rect);
        }

        private static void Assign(SerializedObject so, string path, Type type)
        {
            so.Update();
            SerializedProperty prop = so.FindProperty(path);
            prop.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            so.ApplyModifiedProperties();
        }
    }
}

#endif