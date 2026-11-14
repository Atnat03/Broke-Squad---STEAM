using System;
using UnityEngine;

namespace Vehicle
{
    [Serializable]
    public class WheelBehavior
    {
        public Transform wheelTransform;
        
        //Raycast / Sol
        public float maxRayDistance = 1f;

        //Suspension
        public float suspensionRestDist = 0.5f;
        public float springStrength = 5000f;
        public float springDamper = 800f;

        //Adhérence (grip)
        public float tireGripFactor = 0.8f;
        public float tireMass = 1f;

        //Accélération 
        public bool isMotorWheel = true;
        public AnimationCurve powerCurve;
        public float motorPower = 4000f;
        public float rollingResistance = 0.02f;

        //Direction
        public bool isSteerWheel = false;
        public float maxSteerAngle = 30f;
        public float steerSpeed = 10f;
        
        public RaycastHit tireRaycast;
        private bool _isGrounded;
        private float _currentSteerAngle;
        private Quaternion _initialLocalRotation;
        
        public void SetTire(Quaternion rotation) => _initialLocalRotation = rotation;

        public void ApplySteerVisual(float time, float input)
        {
            float targetAngle = input * maxSteerAngle;
            _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetAngle, time * steerSpeed);

            wheelTransform.localRotation = _initialLocalRotation * Quaternion.Euler(0f, _currentSteerAngle, 0f);
        }
        
        public void SetIsGrounded(bool isGrounded) => _isGrounded = isGrounded;
        public bool IsGrounded() => _isGrounded;
    }
}