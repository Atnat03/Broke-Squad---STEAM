#if UNITY_EDITOR
using UnityEditor;

namespace Gameplay.Items
{
    [CustomEditor(typeof(SO_Item))]
    public class CustomSO_Item : Editor
    {
        private SerializedProperty _id;
        private SerializedProperty _itemName;
        private SerializedProperty _visualPrefab;
        private SerializedProperty _pickUpPrefab;
        private SerializedProperty _icon;
        private SerializedProperty _leftClicks;
        private SerializedProperty _rightClicks;
        private SerializedProperty _conditions;
        private SerializedProperty _passif;

        private void OnEnable()
        {
            _itemName     = serializedObject.FindProperty(nameof(SO_Item.ItemName));
            _visualPrefab = serializedObject.FindProperty(nameof(SO_Item.VisualPrefab));
            _pickUpPrefab = serializedObject.FindProperty(nameof(SO_Item.PickupPrefab));
            _icon         = serializedObject.FindProperty(nameof(SO_Item.Icon));
            _leftClicks   = serializedObject.FindProperty(nameof(SO_Item.FirstActionList));
            _rightClicks  = serializedObject.FindProperty(nameof(SO_Item.SecondActionList));
            _passif       = serializedObject.FindProperty(nameof(SO_Item.PassifList));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_itemName);
            EditorGUILayout.PropertyField(_visualPrefab);
            EditorGUILayout.PropertyField(_pickUpPrefab);
            EditorGUILayout.PropertyField(_icon);

            EditorGUILayout.Space(4);

            SerializeReferenceListDrawer.Draw(_leftClicks,  typeof(IFirstAction),  "First Actions");
            
            EditorGUILayout.Space(4);

            SerializeReferenceListDrawer.Draw(_rightClicks, typeof(ISecondAction), "Second Actions");
            
            EditorGUILayout.Space(4);
            
            SerializeReferenceListDrawer.Draw(_passif,      typeof(IPassif),     "Passifs");

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif