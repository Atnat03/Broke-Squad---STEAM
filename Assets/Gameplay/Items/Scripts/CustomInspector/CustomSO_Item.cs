#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
        private SerializedProperty _firstActions;
        private SerializedProperty _secondActions;
        private SerializedProperty _passif;
        
        private readonly Color _itemColor = new Color(0.1f, 0.3f, 0.2f); 
        private readonly Color _visualColor = new Color(0.3f, 0.25f, 0.15f); 
        private readonly Color _firstActionColor = new Color(0.1f, 0.2f, 0.25f); 
        private readonly Color _secondActionColor = new Color(0.3f, 0.15f, 0.15f); 
        private readonly Color _passifColor = new Color(0.25f, 0.1f, 0.2f);
        
        private void OnEnable()
        {
            _itemName = serializedObject.FindProperty(nameof(SO_Item.ItemName));
            _visualPrefab = serializedObject.FindProperty(nameof(SO_Item.VisualPrefab));
            _pickUpPrefab = serializedObject.FindProperty(nameof(SO_Item.PickupPrefab));
            _icon = serializedObject.FindProperty(nameof(SO_Item.Icon));
            _firstActions = serializedObject.FindProperty(nameof(SO_Item.FirstActionList));
            _secondActions = serializedObject.FindProperty(nameof(SO_Item.SecondActionList));
            _passif = serializedObject.FindProperty(nameof(SO_Item.PassifList));
            
            UpdateAssetIcon();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            //Settings
            BeginSection(_itemColor);
            
            EditorGUILayout.LabelField("Item", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(_itemName);
            EditorGUILayout.PropertyField(_icon);
            EndSection();
            
            //Visuels
            BeginSection(_visualColor);
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_visualPrefab);
            EditorGUILayout.PropertyField(_pickUpPrefab);
            EndSection();
            
            EditorGUILayout.Space(4);
            
            //First action
            BeginSection(_firstActionColor);
            EditorGUILayout.LabelField("First Actions", EditorStyles.boldLabel);
            SerializeReferenceListDrawer.Draw(_firstActions, typeof(IFirstAction), "First Actions");
            EndSection();
            
            //Second Action
            BeginSection(_secondActionColor);
            EditorGUILayout.LabelField("Second Actions", EditorStyles.boldLabel);
            SerializeReferenceListDrawer.Draw(_secondActions, typeof(ISecondAction), "Second Actions");
            EndSection();
            
                        
            //Passif
            BeginSection(_passifColor);
            EditorGUILayout.LabelField("Passif", EditorStyles.boldLabel);
            SerializeReferenceListDrawer.Draw(_passif, typeof(IPassif), "Passif");
            EndSection();
            
            serializedObject.ApplyModifiedProperties();
            
            UpdateAssetIcon();
        }

        private void BeginSection(Color color)
        {
            EditorGUILayout.Space(4);

            Rect rect = EditorGUILayout.BeginVertical();

            rect.x -= 4;
            rect.width += 8;
            
            EditorGUI.DrawRect(rect, color);
            
            GUILayout.Space(4);
        }

        private void EndSection()
        {
            EditorGUILayout.Space(4);

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4);
        }
        
        private void UpdateAssetIcon()
        {
            if (_icon == null)
                return;

            Sprite sprite = _icon.objectReferenceValue as Sprite;

            if (sprite == null)
            {
                EditorGUIUtility.SetIconForObject(target, null);
                return;
            }

            Texture2D texture = sprite.texture;

            if (texture == null)
                return;

            EditorGUIUtility.SetIconForObject(target, texture);

            EditorApplication.RepaintProjectWindow();
        }
    }
}

#endif