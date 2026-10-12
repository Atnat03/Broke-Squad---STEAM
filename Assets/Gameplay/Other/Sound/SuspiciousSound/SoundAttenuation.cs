using System;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Gameplay.Other.SuspiciousSound
{
    public class SoundAttenuation : MonoBehaviour
    {
        [SerializeField] private float _attenuationForce;
        [SerializeField] private float _attenuationPercentage;
        
        enum TypeAttenuation{Raw,Percent}
        
        [SerializeField] private TypeAttenuation _type;

        public float GetAttenuationForce(float currentSoundForce)
        {
            switch (_type)
            {
                case TypeAttenuation.Raw:
                    return currentSoundForce - _attenuationForce;
                case TypeAttenuation.Percent:
                    return currentSoundForce * _attenuationPercentage;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(SoundAttenuation))]
    public class SoundAttenuationCustomInspector : Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _attenuationForce;
        private SerializedProperty _attenuationPercentage;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _attenuationForce = serializedObject.FindProperty("_attenuationForce");
            _attenuationPercentage = serializedObject.FindProperty("_attenuationPercentage");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_type);

            switch (_type.enumValueIndex)
            {
                case 0: // Raw
                    EditorGUILayout.PropertyField(_attenuationForce);
                    break;

                case 1: // Percent
                    EditorGUILayout.PropertyField(_attenuationPercentage);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
}