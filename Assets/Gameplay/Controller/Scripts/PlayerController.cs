using Gameplay.Controller.States;
using UnityEngine;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        #region variables

        private PlayerInput _playerInput;
        private StateMachine _stateMachine;
        public PlayerCamera playerCamera;

        [Header("Speed")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float groundDeceleration = 70f;
        [SerializeField] private float airAcceleration = 12f;

        [Header("Jump/Gravity")]
        [SerializeField] private float jumpHeight = 1.3f;
        [SerializeField] private float gravityMultiplier = 2f;
        [SerializeField] private float fallGravityMultiplier = 1.5f; 
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDist = 0.15f;
        [SerializeField] private float groundCheckRadiusMultiplier = 0.9f;
        [SerializeField] private float maxSlopeAngle = 50f;

        [Header("Crouch")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float crouchTransitionSpeed = 12f;

        private Rigidbody _rb;
        private CapsuleCollider _capsule;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }

        private Vector2 _moveInput;
        private bool _sprintHeld;
        private bool _crouchHeld;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private Vector3 _groundNormal = Vector3.up;

        public void SetYaw(float yawDegrees) => _rb.MoveRotation(Quaternion.Euler(0f, yawDegrees, 0f));

        #endregion

        #region Initialization

        void Awake()
        {
            SetUpComponents();
            SetUpInputs();
            SetUpStateMachine();
        }

        private void SetUpComponents()
        {
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.useGravity = false; 

            
            _capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            _capsule.height = standHeight;
            //RecenterCapsule();
        }

        private void SetUpInputs()
        {
            _playerInput = TryGetComponent(out PlayerInput playerInput) ? playerInput : gameObject.AddComponent<PlayerInput>();

            _playerInput.OnMoveInput += value => _moveInput = value;
            _playerInput.OnJumpInput += () => _jumpBufferTimer = jumpBufferTime;
            _playerInput.OnCrouchInput += () => _crouchHeld = true;
            _playerInput.OnCrouchInputCanceled += () => _crouchHeld = false;
            _playerInput.OnSprintInput += () => _sprintHeld = true;
            _playerInput.OnSprintInputCanceled += () => _sprintHeld = false;
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();

            var movementState = new MovementState(this);
            var sprintState = new SprintState(this);
            var crouchState = new CrouchState(this);
            var fallingState = new FallingState(this);

            At(movementState, sprintState, new FuncPredicate(() => IsSprinting));
            At(sprintState, movementState, new FuncPredicate(() => !IsSprinting));

            At(movementState, crouchState, new FuncPredicate(() => IsCrouching));
            At(sprintState, crouchState, new FuncPredicate(() => IsCrouching));
            At(crouchState, movementState, new FuncPredicate(() => !IsCrouching));

            At(fallingState, movementState, new FuncPredicate(() => IsGrounded));
            Any(fallingState, new FuncPredicate(() => !IsGrounded));

            _stateMachine.SetState(movementState);
        }

        #endregion

        #region Updates

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            CheckGrounded();
            UpdateTimers(dt);
            UpdateCrouch(dt);
            ApplyHorizontalMovement(dt);
            ApplyJump();
            ApplyGravity(dt);
        }

        private void UpdateTimers(float dt)
        {
            _coyoteTimer = IsGrounded ? coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;
        }

        #endregion

        #region Movement

        private void UpdateCrouch(float dt)
        {
            IsCrouching = _crouchHeld || (IsCrouching && !CanStandUp());

            float target = IsCrouching ? crouchHeight : standHeight;
            _capsule.height = Mathf.MoveTowards(_capsule.height, target, crouchTransitionSpeed * dt);
            //RecenterCapsule();
        }

        private bool CanStandUp()
        {
            float radius = _capsule.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
            Vector3 top = transform.position + Vector3.up * (standHeight - radius);
            return !Physics.CheckCapsule(bottom, top, radius, groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void RecenterCapsule()
        {
            Vector3 center = _capsule.center;
            center.y = _capsule.height / 2f; 
            _capsule.center = center;
        }

        private void ApplyHorizontalMovement(float dt)
        {
            IsSprinting = _sprintHeld && !IsCrouching && _moveInput.y > 0.1f;

            float targetSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

            Vector3 input = Vector3.ClampMagnitude(new Vector3(_moveInput.x, 0f, _moveInput.y), 1f);
            Vector3 wishDir = transform.TransformDirection(input);
            Vector3 targetHorizontal = wishDir * targetSpeed;

            float accel;
            if (IsGrounded)
                accel = input.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration;
            else
                accel = airAcceleration;

            Vector3 v = _rb.linearVelocity;
            Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), targetHorizontal, accel * dt);

            _rb.linearVelocity = new Vector3(h.x, v.y, h.z);
        }

        private void ApplyJump()
        {
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !IsCrouching)
            {
                float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
                float jumpVel = Mathf.Sqrt(2f * g * jumpHeight);

                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVel, _rb.linearVelocity.z);

                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                IsGrounded = false;
            }
        }

        private void ApplyGravity(float dt)
        {
            Vector3 v = _rb.linearVelocity;
            bool noInput = _moveInput.sqrMagnitude < 0.01f;
            
            if (IsGrounded && noInput && v.y <= 0.01f) return;

            float mult = gravityMultiplier * (v.y < 0f ? fallGravityMultiplier : 1f);
            v.y += Physics.gravity.y * mult * dt;
            _rb.linearVelocity = v;
        }

        private readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private bool _warnedLayer;

        private void CheckGrounded()
        {
            LayerMask mask = groundLayer;

            float radius = _capsule.radius * groundCheckRadiusMultiplier;
            float startHeight = radius + 0.3f;                       
            Vector3 origin = transform.position + Vector3.up * startHeight;
            float dist = 0.3f + groundCheckDist;                     

            int count = Physics.SphereCastNonAlloc(
                origin, radius, Vector3.down, _groundHits, dist,
                mask, QueryTriggerInteraction.Ignore);

            bool hitGround = false;
            for (int i = 0; i < count; i++)
            {
                var hit = _groundHits[i];
                if (hit.collider == _capsule || hit.collider.transform.IsChildOf(transform)) continue; 
                if (hit.distance <= 0f) continue; 

                if (Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle)
                {
                    _groundNormal = hit.normal;
                    hitGround = true;
                    break;
                }
            }
            
            IsGrounded = hitGround && _rb.linearVelocity.y < 0.1f;

            Debug.DrawRay(origin, Vector3.down * dist, IsGrounded ? Color.green : Color.red);
        }

        #endregion

        void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        private void OnGUI()
        {
            GUI.Label(new Rect(40, 10, 500, 30), $"Grounded = {IsGrounded}", new GUIStyle());
            GUI.Label(new Rect(20, 20, 500, 30), $"State: {_stateMachine.CurrentStateName}", new GUIStyle());
        }
    }
}