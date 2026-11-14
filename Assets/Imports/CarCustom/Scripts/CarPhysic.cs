using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Gameplay.Controller.PlayerInput;

public class CarPhysic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _tireTransform;
    [SerializeField] private Rigidbody _carRigidBody;
    [SerializeField] private Transform _carTransform;
    [SerializeField] private Transform _centerOfMass;

    [Header("Raycast / Sol")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _maxRayDistance = 1f;

    [Header("Suspension")]
    [SerializeField] private float suspensionRestDist = 0.5f;
    [SerializeField] private float springStrength = 5000f;
    [SerializeField] private float springDamper = 800f;

    [Header("Adhérence (grip)")]
    [SerializeField] private float _tireGripFactor = 0.8f;
    [SerializeField] private float _tireMass = 1f;

    [Header("Accélération")] 
    [SerializeField] private bool _isMotorWheel = true;
    [SerializeField] private float _carTopSpeed = 20f;
    [SerializeField] private AnimationCurve _powerCurve;
    [SerializeField] private float _motorPower = 4000f;
    [SerializeField] private float _rollingResistance = 0.02f;

    [Header("Direction")]
    [SerializeField] private bool _isSteerWheel = false;
    [SerializeField] private float _maxSteerAngle = 30f;
    [SerializeField] private float _steerSpeed = 10f;

    private RaycastHit _tireRaycast;
    private bool _isGrounded;
    private float _accelInput;
    private float _steerInput;
    private float _currentSteerAngle;
    private Quaternion _initialLocalRotation;

    //Inputs
    Player _inputs;
    InputAction _moveAction;

    private void Awake()
    {
        _inputs = new Player();
        _inputs.Enable();

        _moveAction = _inputs.Gameplay.Movement;

        _initialLocalRotation = _tireTransform.localRotation;
    }

    private void Update()
    {
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();

        _accelInput = moveValue.y;
        _steerInput = moveValue.x;

        if (_isSteerWheel)
        {
            ApplySteerVisual();
        }
    }

    private void FixedUpdate()
    {
        CastRay();

        if (_isGrounded)
        {
            Suspension();
            Steering();
            Acceleration();
        }
    }

    private void ApplySteerVisual()
    {
        float targetAngle = _steerInput * _maxSteerAngle;
        _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetAngle, Time.deltaTime * _steerSpeed);

        // On tourne la roue autour de son axe vertical local, en partant de sa rotation d'origine
        _tireTransform.localRotation = _initialLocalRotation * Quaternion.Euler(0f, _currentSteerAngle, 0f);
    }

    private void CastRay()
    {
        _isGrounded = Physics.Raycast(
            _tireTransform.position,
            -_tireTransform.up,
            out _tireRaycast,
            _maxRayDistance,
            _groundLayer
        );
    }

    private void Suspension()
    {
        Vector3 springDir = _tireTransform.up;
        Vector3 tireWorldVel = _carRigidBody.GetPointVelocity(_tireTransform.position);

        float offset = suspensionRestDist - _tireRaycast.distance;

        float vel = Vector3.Dot(springDir, tireWorldVel);

        float force = (offset * springStrength) - (vel * springDamper);

        // On applique la force au point de contact réel avec le sol
        _carRigidBody.AddForceAtPosition(springDir * force, _tireRaycast.point);
    }

    private void Steering()
    {
        if (!_isSteerWheel) return;
        
        Vector3 steeringDir = _tireTransform.right;

        Vector3 tireWorldVel = _carRigidBody.GetPointVelocity(_tireTransform.position);

        float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

        float desiredVelChange = -steeringVel * _tireGripFactor;

        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

        _carRigidBody.AddForceAtPosition(steeringDir * _tireMass * desiredAccel, _tireRaycast.point);
    }

    private void Acceleration()
    {
        if (!_isMotorWheel) return;

        Vector3 accelDir = _tireTransform.forward;

        if (Mathf.Abs(_accelInput) > 0.0f)
        {
            float carSpeed = Vector3.Dot(_carTransform.forward, _carRigidBody.linearVelocity);

            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / _carTopSpeed);

            float availableTorque = _powerCurve.Evaluate(normalizedSpeed) * _motorPower * _accelInput;

            _carRigidBody.AddForceAtPosition(accelDir * availableTorque, _tireTransform.position);
        }
        else
        {
            // Pas d'input : résistance de roulement pour ralentir progressivement au lieu de glisser à l'infini
            Vector3 forwardVel = Vector3.Dot(accelDir, _carRigidBody.GetPointVelocity(_tireTransform.position)) * accelDir;
            _carRigidBody.AddForceAtPosition(-forwardVel * _rollingResistance * _carRigidBody.mass, _tireTransform.position);
        }
    }

    // Debug visuel dans la scène
    private void OnDrawGizmos()
    {
        if (_tireTransform == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(_tireTransform.position, _tireTransform.position - _tireTransform.up * _maxRayDistance);
    }
}