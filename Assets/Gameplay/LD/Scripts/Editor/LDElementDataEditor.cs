#if UNITY_EDITOR

using Gameplay.Items.Scripts.CustomInspector;
using Gameplay.LD;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace Gameplay.LD
{
    [CustomEditor(typeof(LDElementData))]
    public class LDElementDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "ModulesList");

            SerializeReferenceListDrawer.Draw(
                serializedObject.FindProperty("ModulesList"),
                typeof(ModuleConfig),
                "Modules");

            AssignMissingIds();

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignMissingIds()
        {
            SerializedProperty list = serializedObject.FindProperty("ModulesList");

            int maxId = 0;

            for(int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue == null) continue;
                SerializedProperty moduleIdProp = element.FindPropertyRelative("ModuleID");
                if (moduleIdProp.intValue > maxId)
                {
                    maxId = moduleIdProp.intValue;
                }
            }

            HashSet<int> usedIds = new HashSet<int>();

            for(int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue == null) continue;
                SerializedProperty moduleIdProp = element.FindPropertyRelative("ModuleID");
                if (moduleIdProp.intValue <= 0 || usedIds.Contains(moduleIdProp.intValue))
                {
                    maxId++;
                    moduleIdProp.intValue = maxId;
                }
                usedIds.Add(moduleIdProp.intValue);
            }
        }
    }
}

#endif