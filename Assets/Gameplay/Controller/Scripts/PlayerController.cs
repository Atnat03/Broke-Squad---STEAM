using UnityEngine;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float acceleration = 40f;

        private PlayerInput _playerInput;
        private Rigidbody _rb;

        private Vector2 _moveInput;
        private bool _sprintHeld;
        private float _yaw;
        
        public void SetYaw(float yawDegrees) => _yaw = yawDegrees;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _playerInput = TryGetComponent(out PlayerInput input)
                ? input
                : gameObject.AddComponent<PlayerInput>();

            _playerInput.OnMoveInput += v => _moveInput = v;
            _playerInput.OnSprintInput += () => _sprintHeld = true;
            _playerInput.OnSprintInputCanceled += () => _sprintHeld = false;
        }

        private void FixedUpdate()
        {
            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            _rb.MoveRotation(yawRot);

            float targetSpeed = _sprintHeld ? sprintSpeed : walkSpeed;

            Vector3 input = Vector3.ClampMagnitude(new Vector3(_moveInput.x, 0f, _moveInput.y), 1f);
            Vector3 targetVel = yawRot * input * targetSpeed;

            Vector3 v = _rb.linearVelocity;
            Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), targetVel, acceleration * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector3(h.x, v.y, h.z);
        }
    }
}