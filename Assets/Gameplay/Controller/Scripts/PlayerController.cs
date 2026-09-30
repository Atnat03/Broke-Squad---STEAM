using System;
using Gameplay.Controller.States;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : NetworkBehaviour
    {
        #region variables

        private PlayerInput _playerInput;
        private StateMachine _stateMachine;
        [SerializeField] private ControllerProfileSO profile;
        
        public bool autoUpdateProfileValue = true;
        
        public PlayerCamera playerCamera;
        public GameObject UI;

        [Header("Speed")]
        private float _walkSpeed = 4f;
        private float _sprintSpeed = 7f;
        private float _crouchSpeed = 2f;
        private float _groundAcceleration = 60f;
        private float _groundDeceleration = 70f;
        private float _airAcceleration = 12f;
                                       
        [Header("Jump/Gravity")]       
        private float _jumpHeight = 1.3f;
        private float _gravityMultiplier = 2f;
        private float _fallGravityMultiplier = 1.5f; 
        private float _coyoteTime = 0.12f;
        private float _jumpBufferTime = 0.12f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDist = 0.15f;
        [SerializeField] private float groundCheckRadiusMultiplier = 0.9f;
        [SerializeField] private float groundCheckSkin = 0.1f;
        [SerializeField] private float maxSlopeAngle = 50f;

        [Header("Crouch")]
        private float _standHeight = 1.8f;
        private float _crouchHeight = 1.0f;
        private float _crouchTransitionSpeed = 12f;

        [Header("Camera")]
        [SerializeField] private Transform eyeTarget;

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
       
        private float _bottomOffsetY;
        private float _eyeStandLocalY;
        
        private float ScaleY => Mathf.Abs(transform.lossyScale.y);
        private float WorldRadius => _capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        private Vector3 FeetPosition
        {
            get
            {
                Vector3 c = transform.TransformPoint(_capsule.center);
                return new Vector3(c.x, c.y - _capsule.height * 0.5f * ScaleY, c.z);
            }
        }

        public void SetYaw(float yawDegrees) => _rb.MoveRotation(Quaternion.Euler(0f, yawDegrees, 0f));
        public Vector3 HorizontalVelocity => new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        #endregion

        #region Initialization

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                UI.SetActive(false);
            }
            else
            {
                SetUpComponents();
                SetUpInputs();
                SetUpStateMachine();
            }
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
            
            _bottomOffsetY = _capsule.center.y - _capsule.height / 2f;

            _capsule.height = _standHeight;
            RecenterCapsule();

            if (eyeTarget != null)
                _eyeStandLocalY = eyeTarget.localPosition.y;
            else
                Debug.LogWarning("PlayerController: eyeTarget is not assigned, the camera won't lower when crouching.", this);
        }

        private void SetUpInputs()
        {
            _playerInput = TryGetComponent(out PlayerInput playerInput) ? playerInput : gameObject.AddComponent<PlayerInput>();

            _playerInput.OnMoveInput += value => _moveInput = value;
            _playerInput.OnJumpInput += () => _jumpBufferTimer = _jumpBufferTime;
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

        private void Update()
        {
            if(!IsOwner)
                return;
            if(autoUpdateProfileValue) GetDataFromProfile();
            _stateMachine.Update();
            UpdateEyeHeight();
        }

        void FixedUpdate()
        {
            if(!IsOwner)
                return;
            
            _stateMachine.FixedUpdate();
            float dt = Time.fixedDeltaTime;

            CheckGrounded();
            UpdateTimers(dt);
            UpdateCrouch(dt);
            ApplyHorizontalMovement(dt);
            ApplyJump();
            ApplyGravity(dt);
        }

        private void LateUpdate()
        {
            if(!IsOwner)
                return;
            
            _stateMachine.LateUpdate();
        }

        private void UpdateTimers(float dt)
        {
            _coyoteTimer = IsGrounded ? _coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;
        }

        #endregion

        void GetDataFromProfile()
        {
            if(profile == null) return;
            _walkSpeed = profile.walkSpeed;
            _sprintSpeed = profile.sprintSpeed;
            _crouchSpeed = profile.crouchSpeed;
            _groundAcceleration = profile.groundAcceleration;
            _groundDeceleration = profile.groundDeceleration;
            _airAcceleration = profile.airAcceleration;
            
            _jumpHeight = profile.jumpHeight; 
            _gravityMultiplier = profile.gravityMultiplier;
            _fallGravityMultiplier = profile.fallGravityMultiplier;
            _coyoteTime = profile.coyoteTime;
            _jumpBufferTime = profile.jumpBufferTime;
            
            _standHeight = profile.standHeight;
            _crouchHeight = profile.crouchHeight;
            _crouchTransitionSpeed = profile.crouchTransitionSpeed;
        }
        
        #region Movement

        private void UpdateCrouch(float dt)
        {
            IsCrouching = _crouchHeld || (IsCrouching && !CanStandUp());

            float target = IsCrouching ? _crouchHeight : _standHeight;
            _capsule.height = Mathf.MoveTowards(_capsule.height, target, _crouchTransitionSpeed * dt);
            RecenterCapsule();
        }
        
        private void UpdateEyeHeight()
        {
            if (eyeTarget == null) return;

            float targetHeight = IsCrouching ? _crouchHeight : _standHeight;
            float targetY = _eyeStandLocalY - (_standHeight - targetHeight);

            Vector3 p = eyeTarget.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetY, _crouchTransitionSpeed * Time.deltaTime);
            eyeTarget.localPosition = p;
        }

        private bool CanStandUp()
        {
            float radius = WorldRadius * 0.95f;
            Vector3 feet = FeetPosition;
            Vector3 bottom = feet + Vector3.up * (radius + 0.05f);
            Vector3 top = feet + Vector3.up * (_standHeight * ScaleY - radius);
            return !Physics.CheckCapsule(bottom, top, radius, groundLayer, QueryTriggerInteraction.Ignore);
        }
        
        private void RecenterCapsule()
        {
            Vector3 center = _capsule.center;
            center.y = _bottomOffsetY + _capsule.height / 2f; 
            _capsule.center = center;
        }

        private void ApplyHorizontalMovement(float dt)
        {
            IsSprinting = _sprintHeld && !IsCrouching && _moveInput.y > 0.1f;

            float targetSpeed = IsCrouching ? _crouchSpeed : (IsSprinting ? _sprintSpeed : _walkSpeed);

            Vector3 input = Vector3.ClampMagnitude(new Vector3(_moveInput.x, 0f, _moveInput.y), 1f);
            Vector3 wishDir = transform.TransformDirection(input);
            Vector3 targetHorizontal = wishDir * targetSpeed;

            float accel;
            if (IsGrounded)
                accel = input.sqrMagnitude > 0.01f ? _groundAcceleration : _groundDeceleration;
            else
                accel = _airAcceleration;

            Vector3 v = _rb.linearVelocity;
            Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), targetHorizontal, accel * dt);

            _rb.linearVelocity = new Vector3(h.x, v.y, h.z);
        }

        private void ApplyJump()
        {
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !IsCrouching)
            {
                float g = Mathf.Abs(Physics.gravity.y) * _gravityMultiplier;
                float jumpVel = Mathf.Sqrt(2f * g * _jumpHeight);

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

            float mult = _gravityMultiplier * (v.y < 0f ? _fallGravityMultiplier : 1f);
            v.y += Physics.gravity.y * mult * dt;
            _rb.linearVelocity = v;
        }

        private readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private bool _warnedLayer;

        private void CheckGrounded()
        {
            LayerMask mask = groundLayer;
            
            float radius = WorldRadius * groundCheckRadiusMultiplier;
            Vector3 origin = FeetPosition + Vector3.up * (radius + groundCheckSkin);
            float dist = groundCheckSkin + groundCheckDist;

            int count = Physics.SphereCastNonAlloc(
                origin, radius, Vector3.down, _groundHits, dist,
                mask, QueryTriggerInteraction.Ignore);

            bool hitGround = false;
            for (int i = 0; i < count; i++)
            {
                var hit = _groundHits[i];
                if (hit.collider == _capsule || hit.collider.transform.IsChildOf(transform)) continue; 
                
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
            //GUI.Label(new Rect(20, 20, 500, 30), $"State: {_stateMachine.CurrentStateName}", new GUIStyle());
        }
    }
}