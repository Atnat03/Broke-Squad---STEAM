#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [CustomPropertyDrawer(typeof(SerializeReferencePickerAttribute))]
    public class SerializeReferencePickerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ManagedReference) return height;
            if (IsNull(property) || !property.isExpanded) return height;

            foreach (SerializedProperty child in Children(property))
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "À utiliser avec [SerializeReference]");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            
            Color backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.25f);

            Rect backgroundRect = new Rect(
                position.x,
                position.y + 2,
                position.width,
                position.height
            );

            EditorGUI.DrawRect(backgroundRect, backgroundColor);
            
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            var labelRect  = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, line);
            var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                      position.width - EditorGUIUtility.labelWidth, line);

            bool isNull = IsNull(property);

            if (isNull) EditorGUI.LabelField(labelRect, label);
            else property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(TypeLabel(property)), FocusType.Keyboard))
                ShowMenu(buttonRect, property.Copy());

            if (!isNull && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + line + spacing;

                foreach (SerializedProperty child in Children(property))
                {
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                    y += h + spacing;
                }

                EditorGUI.indentLevel--;
            }
            
            GUILayout.Space(16);

            EditorGUI.EndProperty();
        }

        private void ShowMenu(Rect rect, SerializedProperty property)
        {
            string current = property.managedReferenceFullTypename;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(current), () => Assign(property, null));

            foreach (Type t in TypeCache.GetTypesDerivedFrom(fieldInfo.FieldType).Where(IsValid).OrderBy(t => t.Name))
            {
                Type type = t;
                bool selected = current == $"{type.Assembly.GetName().Name} {type.FullName}";
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), selected, () => Assign(property, type));
            }

            menu.DropDown(rect);
        }

        private static void Assign(SerializedProperty property, Type type)
        {
            property.serializedObject.Update();
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.isExpanded = true;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static bool IsValid(Type t) =>
            !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition &&
            t.IsSerializable && !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
            t.GetConstructor(Type.EmptyTypes) != null;

        private static bool IsNull(SerializedProperty p) => string.IsNullOrEmpty(p.managedReferenceFullTypename);

        private static string TypeLabel(SerializedProperty p)
        {
            string full = p.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return "None";

            string name = full.Substring(full.IndexOf(' ') + 1);
            name = name.Substring(name.LastIndexOf('.') + 1);
            return ObjectNames.NicifyVariableName(name);
        }

        private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            SerializedProperty it = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enter = true;

            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = false;
                yield return it.Copy();
            }
        }
    }
}
#endif