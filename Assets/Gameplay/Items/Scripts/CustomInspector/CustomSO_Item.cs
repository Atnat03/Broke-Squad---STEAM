#if UNITY_EDITOR

using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using UnityEditor;

namespace Gameplay.Items.Scripts.CustomInspector
{
    [CustomEditor(typeof(SO_Item))]
    public class CustomSO_Item : Editor
    {
        private SerializedProperty _id;
        private SerializedProperty _itemName;
        private SerializedProperty _visualPrefab;
        private SerializedProperty _leftClicks;
        private SerializedProperty _rightClicks;
        private SerializedProperty _conditions;
        private SerializedProperty _passif;

        private void OnEnable()
        {
            _id           = serializedObject.FindProperty(nameof(SO_Item.id));
            _itemName     = serializedObject.FindProperty(nameof(SO_Item.itemName));
            _visualPrefab = serializedObject.FindProperty(nameof(SO_Item.visualPrefab));
            _leftClicks   = serializedObject.FindProperty(nameof(SO_Item.leftClicksActions));
            _rightClicks  = serializedObject.FindProperty(nameof(SO_Item.rightClicksActions));
            _conditions   = serializedObject.FindProperty(nameof(SO_Item.conditions));
            _passif       = serializedObject.FindProperty(nameof(SO_Item.passif));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_id);
            EditorGUILayout.PropertyField(_itemName);
            EditorGUILayout.PropertyField(_visualPrefab);

            EditorGUILayout.Space();

            SerializeReferenceListDrawer.Draw(_leftClicks,  typeof(ILeftClick),  "Left Click Actions");
            SerializeReferenceListDrawer.Draw(_rightClicks, typeof(IRightClick), "Right Click Actions");
            SerializeReferenceListDrawer.Draw(_conditions,  typeof(ICondition),  "Conditions");
            SerializeReferenceListDrawer.Draw(_passif,      typeof(IPassif),     "Passifs");

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif