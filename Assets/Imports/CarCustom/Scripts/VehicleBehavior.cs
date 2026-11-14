using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Vehicle
{
    public enum CarType
    {Propulsion, Traction, _4x4}
    
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleBehavior : NetworkBehaviour
    {
        #region Properties

        public List<WheelBehavior> WheelsData => _wheelList;

        #endregion
        
        #region Variables
        
        [SerializeField] private Transform _centerOfMass;
        private Rigidbody _rigidbody;
        
        [SerializeField] private List<WheelBehavior> _wheelList;
        
        [SerializeField] private CarType _type = CarType.Propulsion;
        [SerializeField] private float _carMaxSpeed = 20;
        [SerializeField] private LayerMask _groundLayer;

        //Inputs
        Player _inputs;
        InputAction _moveAction;
        float _accelInput;
        float _steerInput;
        
        #endregion

        #region Functions
        
        #region Seting Up
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            
            _inputs = new Player();
            _inputs.Enable();

            _moveAction = _inputs.Gameplay.Movement;
            
            _rigidbody.centerOfMass = _centerOfMass.localPosition;
            
            SetUpTires();
            SetUpMotorType();
        }

        void SetUpTires()
        {
            foreach (WheelBehavior wheel in _wheelList)
            {
                wheel.SetTire(wheel.wheelTransform.transform.localRotation);
                wheel.isSteerWheel = false;
            }

            _wheelList[0].isSteerWheel = true;
            _wheelList[1].isSteerWheel = true;
        }

        #endregion
        
        #region Updates

        private void Update()
        {
            if (IsOwner)
            {
            }
            
            GetInputs();
            
            ApplyVisualWheels();
        }

        private void FixedUpdate()
        {
            CastRay();

            ApplyWheelsPhysic();
        }

        #endregion

        void GetInputs()
        {
            Vector2 moveValue = _moveAction.ReadValue<Vector2>();
            _accelInput = moveValue.y;
            _steerInput = moveValue.x;
        }
        
        
        private void ApplyVisualWheels()
        {
            foreach (WheelBehavior wheel in _wheelList)
            {
                if (wheel.isSteerWheel)
                {
                    wheel.ApplySteerVisual(Time.deltaTime, _steerInput);
                }
            }
        }

        private void CastRay()
        {
            foreach (WheelBehavior wheel in _wheelList)
            {
                wheel.SetIsGrounded(Physics.Raycast(
                    wheel.wheelTransform.position,
                    -wheel.wheelTransform.up,
                    out wheel.tireRaycast,
                    wheel.maxRayDistance,
                    _groundLayer
                ));
            }
        }

        private void ApplyWheelsPhysic()
        {
            foreach (WheelBehavior wheel in _wheelList)
            {
                if (wheel.IsGrounded())
                {
                    Suspension(wheel);
                    Steering(wheel);
                    Acceleration(wheel);
                }
            }
        }

        private void Suspension(WheelBehavior wheel)
        {
            Vector3 springDir = wheel.wheelTransform.up;

            Vector3 tireWorldVel = _rigidbody.GetPointVelocity(wheel.tireRaycast.point);

            float compression = Mathf.Clamp(wheel.suspensionRestDist - wheel.tireRaycast.distance, 0f, wheel.maxSuspensionCompression);

            float velocity = Vector3.Dot(springDir, tireWorldVel);

            float force = compression * wheel.springStrength - velocity * wheel.springDamper;

            force = Mathf.Max(0f, force);

            _rigidbody.AddForceAtPosition(springDir * force, wheel.tireRaycast.point);
        }
        
        private void Steering(WheelBehavior wheel)
        {
            Vector3 steeringDir = wheel.wheelTransform.right;
            
            Vector3 tireWorldVel = _rigidbody.GetPointVelocity(wheel.wheelTransform.position);
            
            float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

            float desiredVelChange = -steeringVel * wheel.tireGripFactor;
            
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            
            _rigidbody.AddForceAtPosition(steeringDir * wheel.tireMass * desiredAccel, wheel.tireRaycast.point);
        }

        private void Acceleration(WheelBehavior wheel)
        {
            if (!wheel.isMotorWheel) return;
            
            Vector3 accelDir = transform.forward;

            //Acceleration en fonction de l'input
            if (Mathf.Abs(_accelInput) > 0.1f)
            {
                float carSpeed = Vector3.Dot(transform.forward, _rigidbody.linearVelocity);
                
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / _carMaxSpeed);
                float availableTorque = wheel.powerCurve.Evaluate(normalizedSpeed) * wheel.motorPower * _accelInput;
                
                _rigidbody.AddForceAtPosition(accelDir * availableTorque, wheel.wheelTransform.position);
            }
            else
            {
                //Frein moteur
                Vector3 forwardVel = Vector3.Dot(accelDir, _rigidbody.GetPointVelocity(wheel.wheelTransform.position)) * accelDir;
                _rigidbody.AddForceAtPosition(-forwardVel * wheel.rollingResistance * _rigidbody.mass, wheel.wheelTransform.position);
            }
        }

        public void SetUpMotorType()
        {
            foreach (WheelBehavior wheel in _wheelList)
            {
                wheel.isMotorWheel = false;
            }
            
            switch (_type)
            {
                case CarType.Propulsion :
                    _wheelList[2].isMotorWheel = true;
                    _wheelList[3].isMotorWheel = true;
                    break;
                case CarType.Traction :
                    _wheelList[0].isMotorWheel = true;
                    _wheelList[1].isMotorWheel = true;
                    break;
                case CarType._4x4 :
                    foreach (WheelBehavior wheel in _wheelList)
                    {
                        wheel.isMotorWheel = true;
                    }
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            if(_wheelList.Count == 0) return;
            
            foreach (WheelBehavior wheel in _wheelList)
            {
                if (wheel.wheelTransform == null) return;

                Gizmos.color = wheel.IsGrounded() ? Color.green : Color.red;
                Gizmos.DrawLine(wheel.wheelTransform.position, wheel.wheelTransform.position - wheel.wheelTransform.up * wheel.maxRayDistance);
            }
        }
        
        #endregion

    }
}

