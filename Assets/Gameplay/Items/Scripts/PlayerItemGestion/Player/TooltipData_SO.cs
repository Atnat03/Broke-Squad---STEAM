using System;
using System.Collections.Generic;
using System.Linq;
using MyPrint;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    [CreateAssetMenu(fileName = "TooltipData", menuName = "Items/Tooltip Data")]
    public class TooltipData_SO : ScriptableObject
    {
        [SerializeField] public string TooltipTextInteractable = string.Empty;

        [Header("Is Item")] 
        [SerializeField] public bool IsItem = false;
        [SerializeField] public string TooltipItemInHand = string.Empty;
        
        [Header("Interactable with item")]
        [SerializeField] public bool IsInteractableWithItem = false;
        [SerializeField] public string TooltipInteractableWithItem = string.Empty;
        [SerializeReference] public ItemModule ModuleNeedToInteract;
        
        public string GetCurrentString(ItemModule[] modules)
        {
            if(!IsInteractableWithItem)
                return TooltipTextInteractable;

            foreach (ItemModule module in modules)
            {
                if (ModuleNeedToInteract.GetType() == module.GetType())
                {
                    return TooltipInteractableWithItem;
                }
            }
            
            return TooltipTextInteractable;
        }
    }
    
    
#if UNITY_EDITOR
    [CustomEditor(typeof(TooltipData_SO))]
    public class TooltipData_SO_CustomInspector : Editor
    {
        private SerializedProperty _tooltipBase;
        private SerializedProperty _isItem;
        private SerializedProperty _tooltipItemInHand;
        private SerializedProperty _isInteractableWithItemInHand;
        private SerializedProperty _tooltipTextItemInHand;
        private SerializedProperty _moduleNeedToInteract;

        private List<Type> _moduleTypes;
        private string[] _moduleNames;

        private void OnEnable()
        {
            _tooltipBase = serializedObject.FindProperty("TooltipTextInteractable");
            _isItem = serializedObject.FindProperty("IsItem");
            _tooltipItemInHand = serializedObject.FindProperty("TooltipItemInHand");

            _isInteractableWithItemInHand =
                serializedObject.FindProperty("IsInteractableWithItem");

            _tooltipTextItemInHand =
                serializedObject.FindProperty("TooltipInteractableWithItem");

            _moduleNeedToInteract =
                serializedObject.FindProperty("ModuleNeedToInteract");

            FindAllModuleTypes();
        }

        private void FindAllModuleTypes()
        {
            _moduleTypes = TypeCache.GetTypesDerivedFrom<ItemModule>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();

            _moduleNames = new string[_moduleTypes.Count + 1];
            _moduleNames[0] = "None";

            for (int i = 0; i < _moduleTypes.Count; i++)
            {
                _moduleNames[i + 1] = _moduleTypes[i].Name;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_tooltipBase);

            EditorGUILayout.PropertyField(_isItem);

            if (_isItem.boolValue)
            {
                EditorGUILayout.PropertyField(_tooltipItemInHand);
            }

            EditorGUILayout.PropertyField(_isInteractableWithItemInHand);

            if (_isInteractableWithItemInHand.boolValue)
            {
                EditorGUILayout.PropertyField(_tooltipTextItemInHand);

                DrawModuleDropdown();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModuleDropdown()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Module required to interact", EditorStyles.boldLabel);

            int selectedIndex = 0;

            if (_moduleNeedToInteract.managedReferenceValue != null)
            {
                Type currentType = _moduleNeedToInteract.managedReferenceValue.GetType();
                int index = _moduleTypes.IndexOf(currentType);

                if (index >= 0)
                    selectedIndex = index + 1;
            }

            int newIndex = EditorGUILayout.Popup("Required Module", selectedIndex, _moduleNames);

            if (newIndex != selectedIndex)
            {
                if (newIndex == 0)
                {
                    _moduleNeedToInteract.managedReferenceValue = null;
                }
                else
                {
                    Type selectedType = _moduleTypes[newIndex - 1];

                    _moduleNeedToInteract.managedReferenceValue =
                        Activator.CreateInstance(selectedType);
                }
            }
        }
    }
#endif
}