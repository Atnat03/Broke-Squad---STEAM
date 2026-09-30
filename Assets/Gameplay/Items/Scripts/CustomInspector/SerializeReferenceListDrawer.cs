#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items.Scripts.CustomInspector
{
    public static class SerializeReferenceListDrawer
    {
        private static readonly Dictionary<Type, Type[]> TypeCache = new();

        /// <summary>Retourne tous les types concrets implémentant baseType.</summary>
        public static Type[] GetImplementations(Type baseType)
        {
            if (TypeCache.TryGetValue(baseType, out var cached))
                return cached;

            var types = UnityEditor.TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract
                            && !t.IsInterface
                            && !t.IsGenericType
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null) // ctor public sans param
                .OrderBy(t => t.Name)
                .ToArray();

            TypeCache[baseType] = types;
            return types;
        }

        public static void Draw(SerializedProperty list, Type baseType, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // En-tête + bouton d'ajout
            EditorGUILayout.BeginHorizontal();
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, $"{label} ({list.arraySize})", true);

            if (GUILayout.Button("+ Ajouter", GUILayout.Width(80)))
                ShowAddMenu(list, baseType);

            EditorGUILayout.EndHorizontal();

            if (list.isExpanded)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    string typeName = GetTypeDisplayName(element);
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, typeName, true);

                    // Monter / descendre / supprimer
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(24)))
                    {
                        list.MoveArrayElement(i, i - 1);
                        break;
                    }

                    GUI.enabled = i < list.arraySize - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(24)))
                    {
                        list.MoveArrayElement(i, i + 1);
                        break;
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        // Sur un élément de type reference, un 1er Delete met la valeur à null,
                        // un 2e supprime réellement l'entrée
                        list.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (element.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawChildren(element);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawChildren(SerializedProperty element)
        {
            if (element.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Référence nulle.", MessageType.Warning);
                return;
            }

            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }

        private static void ShowAddMenu(SerializedProperty list, Type baseType)
        {
            var menu = new GenericMenu();
            Type[] types = GetImplementations(baseType);

            if (types.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("Aucune implémentation trouvée"));
            }
            else
            {
                // Les SerializedProperty ne survivent pas au callback : on passe par le targetObject
                SerializedObject so = list.serializedObject;
                string path = list.propertyPath;

                foreach (Type type in types)
                {
                    Type captured = type;
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        so.Update();
                        SerializedProperty prop = so.FindProperty(path);

                        int index = prop.arraySize;
                        prop.arraySize++;
                        prop.GetArrayElementAtIndex(index).managedReferenceValue =
                            Activator.CreateInstance(captured);

                        so.ApplyModifiedProperties();
                    });
                }
            }

            menu.ShowAsContext();
        }

        private static string GetTypeDisplayName(SerializedProperty element)
        {
            string full = element.managedReferenceFullTypename; // "Assembly Namespace.Type"
            if (string.IsNullOrEmpty(full))
                return "Vide (null)";

            string typeName = full.Substring(full.LastIndexOf(' ') + 1);
            return ObjectNames.NicifyVariableName(typeName.Substring(typeName.LastIndexOf('.') + 1));
        }
    }
}

#endif