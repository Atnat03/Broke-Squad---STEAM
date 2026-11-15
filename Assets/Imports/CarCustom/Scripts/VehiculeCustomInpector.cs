#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vehicle
{
    [CustomEditor(typeof(VehicleBehavior))]
    public class VehicleCustomInspector : Editor
    {
        private VehicleBehavior _car;
        private SerializedProperty _wheelsProperty;
        private int _selectedWheel = -1;
        
        readonly string[] _nameWheels = {"Front Left", "Front Right", "Back Left", "Back Right"};
        
        //Textures
        private Texture2D _carTexture;
        private Texture2D _wheelIconTexture;
        
        //Styles
        private GUIStyle centeredLabelStyle;

        private void OnEnable()
        {
            _wheelsProperty = serializedObject.FindProperty("_wheelList");
            
            _carTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imports/CarCustom/Scripts/Icon/Car.png");
            _wheelIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imports/CarCustom/Scripts/Icon/Wheel.png");

            centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _car = (VehicleBehavior)target;
            
            GUILayout.Label("Vehicle Builder", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_centerOfMass"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_groundLayer"));
            
            GUILayout.Space(4);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_carMaxSpeed"));

            DrawInteractableCar();

            if (_wheelsProperty != null &&
                _selectedWheel >= 0 &&
                _selectedWheel < _wheelsProperty.arraySize)
            {
                DrawOneTireData(_wheelsProperty.GetArrayElementAtIndex(_selectedWheel), _car.WheelsData[_selectedWheel]);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawInteractableCar()
        {
            if (_wheelsProperty == null) return;
            
            float areaSize = 256f;
            Rect area = GUILayoutUtility.GetRect(areaSize, areaSize);
            
            //Car Image
            float carSize = areaSize * 0.5f;
            Rect carRect = new Rect(
                area.x + (areaSize - carSize) / 2f,
                area.y + (areaSize - carSize) / 2f,
                carSize,
                carSize);
            
            if(_carTexture != null)
                GUI.DrawTexture(carRect, _carTexture, ScaleMode.ScaleToFit);
            
            //Wheels Buttons
            float buttonSize = 32f;
            Rect[] corners =
            {
                //FR
                new Rect(
                    area.x + buttonSize * 2, area.y + buttonSize * 2,
                    buttonSize, buttonSize),
                
                //FL
                new Rect(
                    area.x + areaSize/2 + buttonSize, area.y + buttonSize*2, 
                    buttonSize, buttonSize),
                
                //BR
                new Rect(
                    area.x + buttonSize * 2, area.y + areaSize - buttonSize * 3,
                    buttonSize, buttonSize),
                
                //BL
                new Rect(
                    area.x + areaSize/2 + buttonSize, area.y + areaSize - buttonSize * 3, 
                    buttonSize, buttonSize)
            };
            
            int count = Mathf.Min(4, _wheelsProperty.arraySize);
            for (int i = 0; i < count; i++)
            {
                GUIContent content = _wheelIconTexture != null
                    ? new GUIContent(_wheelIconTexture, $"Roue {i}")
                    : new GUIContent(i.ToString());

                Color previousColor = GUI.backgroundColor;

                if (_selectedWheel == i)
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);

                if (GUI.Button(corners[i], content))
                {
                    _selectedWheel = i;
                }

                GUI.backgroundColor = previousColor;
            }
            
            CustomDraw.DrawOutline(carRect, Color.black, 1);

            EditorGUI.BeginChangeCheck();
            CustomDraw.DrawEnumDropdown(serializedObject.FindProperty("_type"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                Undo.RecordObject(_car, "Change Car Type");
                _car.SetUpMotorType();
                EditorUtility.SetDirty(_car);
                
                serializedObject.Update();
            }
        }

        private void DrawOneTireData(SerializedProperty wheelElement, WheelBehavior data)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField($"Wheel {_nameWheels[_selectedWheel]}", centeredLabelStyle);

            GUILayout.Space(4);
                
            GUILayout.Label("Steering", centeredLabelStyle);
                
            GUILayout.Space(4);
            
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("wheelTransform"));
            
            GUILayout.Space(4);
            
            GUILayout.Label("Suspensions", centeredLabelStyle);
                
            GUILayout.Space(4);
            
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("maxRayDistance"));
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("suspensionRestDist"));
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("springStrength"));
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("springDamper"));
            
            GUILayout.Space(4);
                
            GUILayout.Label("Grip", centeredLabelStyle);
                
            GUILayout.Space(4);
            
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("tireGripFactor"));
            EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("tireMass"));
            
            if(data.isMotorWheel)
            {
                GUILayout.Space(4);
                
                GUILayout.Label("Motor", centeredLabelStyle);
                
                GUILayout.Space(4);
                
                EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("powerCurve"));
                EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("motorPower"));
                EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("rollingResistance"));
            }
            
            if(data.isSteerWheel)
            {
                GUILayout.Space(4);
                
                GUILayout.Label("Steering", centeredLabelStyle);
                
                GUILayout.Space(4);
                
                EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("maxSteerAngle"));
                EditorGUILayout.PropertyField(wheelElement.FindPropertyRelative("steerSpeed"));
            }
        }
    }
}

#endif