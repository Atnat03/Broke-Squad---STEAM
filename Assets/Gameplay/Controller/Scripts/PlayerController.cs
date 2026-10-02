using System;
using Bus;
using Gameplay.Controller.States;
using Gameplay.PlayerData;
using Network.Connections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : NetworkBusListener
    {
        #region variables

        private PlayerInput _playerInput;
        private StateMachine _stateMachine;
        
        [Header("Profile")]
        [SerializeField] private ControllerProfileSO profile;
        public bool autoUpdateProfileValue = true;
        private int _syncedColor;
        private string _syncedName = "";
        
        [Header("To Assign")]
        public PlayerCamera playerCamera;
        public GameObject UI;
        public MeshRenderer[] meshRenderer;
        public TextMeshProUGUI pseudoText;
        

        [Header("Speed")]
        private float _walkSpeed = 4f;
        private float _sprintSpeed = 7f;
        private float _crouchSpeed = 2f;
        private float _backwardSpeedMultiplier = 0.7f;
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
        [SerializeField] private LayerMask ceilingLayer;

        [Header("Stamina")]
        private float _maxStamina = 100f;
        private float _drainPerSecond = 10f;
        private float _regenPerSecond = 10f;
        private float _regenDelay = 2f;

        [Header("Tied Up")] 
        [SerializeField] private float tiedUpSpeed = 1f;
        [SerializeField] private float tiedUpHeight = 0.8f;
        private bool _playerDead;
        public bool IsDown => _playerDead;
        
        [Header("Camera")]
        [SerializeField] private Transform eyeTarget;

        [Header("SFX")] 
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float sprintStepInterval = 0.35f;
        [SerializeField] private float crouchStepInterval = 0.8f;
        [SerializeField] private float walkStepVolume = 0.3f;
        [SerializeField] private float sprintStepVolume = 0.5f;
        [SerializeField] private float crouchStepVolume = 0.1f;
        [SerializeField, SoundName] private string[] _walkSound;
        [SerializeField, SoundName] private string _jumpSound;
        [SerializeField, SoundName] private string _crouchSound;
        
        [Header("Visual")]
        [SerializeField] private Transform visual;
        [SerializeField] private GameObject tiedVisual;
        private Vector3 _visualBaseScale = Vector3.one;
        private float _initialHeight;
        private Rigidbody _rb;
        private CapsuleCollider _capsule;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }

        private Vector2 _moveInput;
        private bool _sprintHeld;
        private bool _crouchHeld;
        private bool _leanLeftHeld;
        private bool _leanRightHeld;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        
        private float _stamina;
        private float _regenTimer;
        private bool _exhausted;
        
        private Vector3 _groundNormal = Vector3.up;
       
        private float _bottomOffsetY;
        private float _eyeStandLocalY;
        
        private float ScaleY => Mathf.Abs(transform.lossyScale.y);
        private float WorldRadius => _capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        public float LeanInput => (_leanRightHeld ? 1f : 0f) - (_leanLeftHeld ? 1f : 0f);
        public float ForwardDot
        {
            get
            {
                Vector3 v = HorizontalVelocity;
                if (v.sqrMagnitude < 0.01f) return 0f;
                return Vector3.Dot(v.normalized, transform.forward);
            }
        }

        public bool IsMovingForward => ForwardDot > 0.1f;
        public bool IsMovingBackward => ForwardDot < -0.1f;
        
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
        
        public float MaxStamina => _maxStamina;

        private float _lastSentStamina = -1f;
        
        private readonly NetworkVariable<bool> _netCrouching = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _netDown = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        #endregion

        
        #region Initialization

        public override void OnNetworkSpawn()
        {
            SetUpComponents();
            
            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                UI.SetActive(false);
                RequestPersonalisationRpc();
            }
            else
            {
                foreach (var mesh in meshRenderer) mesh.enabled = false;
                pseudoText.gameObject.SetActive(false);
                
                SendPersonalisation();
                
                SetUpInputs();
                SetUpStateMachine();
                ListenToEvent<PlayerDeathEvent>(SetPlayerDead);
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
            
            _stamina = _maxStamina;
            
            _capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            
            _bottomOffsetY = _capsule.center.y - _capsule.height / 2f;
            _initialHeight = _capsule.height;
            if (visual != null) _visualBaseScale = visual.localScale;
            
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
            _playerInput.OnLeanRightInput += () => _leanRightHeld = true;
            _playerInput.OnLeanLeftInput += () => _leanLeftHeld = true;
            _playerInput.OnLeanRightInputCanceled += () => _leanRightHeld = false;
            _playerInput.OnLeanLeftInputCanceled += () => _leanLeftHeld = false;
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
            if(autoUpdateProfileValue) GetDataFromProfile();
            if (!IsOwner)
            {
                UpdateBodyShape(Time.deltaTime, _netCrouching.Value, _netDown.Value);
                return;
            }
            
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
            UpdateStamina(dt);
            ApplyJump();
            ApplyGravity(dt);
            UpdateFootsteps(dt);
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

        private void UpdateStamina(float dt)
        {
            if (IsSprinting)
            {
                _stamina = Mathf.Max(0f, _stamina - _drainPerSecond * dt);
                _regenTimer = _regenDelay;
                
                if(_stamina <= 0f) _exhausted = true;
            }
            else
            {
                if (_regenTimer > 0f)
                {
                    _regenTimer -= dt;
                }
                else
                {
                    _stamina = Mathf.Min(_maxStamina, _stamina + _regenPerSecond * dt);
                }
            }
            
            if(_exhausted && _stamina > 0) _exhausted = false;
            
            NotifyStamina();
        }

        #endregion

        private void NotifyStamina()
        {
            bool changedEnough = Mathf.Abs(_stamina - _lastSentStamina) >= _maxStamina * 0.01f;
            bool atBoundary = (_stamina <= 0f || _stamina >= _maxStamina) && !Mathf.Approximately(_stamina, _lastSentStamina);

            if (!changedEnough && !atBoundary) return;

            _lastSentStamina = _stamina;
            InvokeEvent(new StaminaChangedEvent { stamina = _stamina, maxStamina = _maxStamina });
        }
        void GetDataFromProfile()
        {
            if(profile == null) return;
            _walkSpeed = profile.walkSpeed;
            _sprintSpeed = profile.sprintSpeed;
            _crouchSpeed = profile.crouchSpeed;
            _backwardSpeedMultiplier = profile.backwardSpeedMultiplier;
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
            
            _maxStamina = profile.maxStamina;
            _drainPerSecond = profile.drainPerSecond;
            _regenPerSecond = profile.regenPerSecond;
            _regenDelay = profile.regenDelay;
        }
        
        private void UpdateBodyShape(float dt, bool crouching, bool down)
        {
            float target = down ? tiedUpHeight : (crouching ? _crouchHeight : _standHeight);
            _capsule.height = Mathf.MoveTowards(_capsule.height, target, _crouchTransitionSpeed * dt);
            RecenterCapsule();
            tiedVisual.SetActive(down);
            if (visual != null)
            {
                float ratio = _capsule.height / _initialHeight;
                visual.localScale = new Vector3(_visualBaseScale.x, _visualBaseScale.y * ratio, _visualBaseScale.z);
                Vector3 p = visual.localPosition;
                p.y = _bottomOffsetY + _capsule.height / 2f;
                visual.localPosition = p;
            }
        }
        
        #region Movement

        private void UpdateCrouch(float dt)
        {
            if(!IsCrouching && _crouchHeld || (IsCrouching && !CanStandUp())) PlaySound(_crouchSound, 0.5f);
            IsCrouching = _crouchHeld || (IsCrouching && !CanStandUp());
            IsCrouching = !_playerDead && IsCrouching;
            
            _netCrouching.Value = IsCrouching;
            _netDown.Value = _playerDead;
            UpdateBodyShape(dt, IsCrouching, _playerDead);
        }
        
        private void UpdateEyeHeight()
        {
            if (eyeTarget == null) return;

            float targetHeight = IsCrouching ? _crouchHeight : _standHeight;
            targetHeight = _playerDead ? tiedUpHeight : targetHeight;
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
            Debug.DrawLine(bottom, top, Color.red);
            return !Physics.CheckCapsule(bottom, top, radius, ceilingLayer, QueryTriggerInteraction.Ignore);
        }
        
        private void RecenterCapsule()
        {
            Vector3 center = _capsule.center;
            center.y = _bottomOffsetY + _capsule.height / 2f; 
            _capsule.center = center;
        }

        private void ApplyHorizontalMovement(float dt)
        {
            IsSprinting = _sprintHeld && !IsCrouching && _moveInput.y > 0.1f && !_exhausted && _stamina > 0f;
            IsSprinting = IsSprinting &&  !_playerDead;
            
            
            float targetSpeed = IsCrouching ? _crouchSpeed : (IsSprinting ? _sprintSpeed : _walkSpeed);
            targetSpeed = _playerDead ? tiedUpSpeed : targetSpeed;
            targetSpeed *= IsMovingBackward ? _backwardSpeedMultiplier : 1f;
            
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
            
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !IsCrouching && !_playerDead)
            {
                float g = Mathf.Abs(Physics.gravity.y) * _gravityMultiplier;
                float jumpVel = Mathf.Sqrt(2f * g * _jumpHeight);

                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVel, _rb.linearVelocity.z);

                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                IsGrounded = false;
                PlaySound(_jumpSound, 0.5f);
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

        void SetPlayerDead(PlayerDeathEvent e)
        {
            if (e.playerID == OwnerClientId)
            {
                _playerDead = true;
                _rb.linearVelocity = Vector3.zero;
            }
            
        }

        public void Revive()
        {
            _playerDead = false;
        }
        
        private float stepTimer;
        private bool _wasMoving;

        private void UpdateFootsteps(float dt)
        {
            bool isMoving = IsGrounded && _moveInput.sqrMagnitude > 0.01f;

            if (!isMoving)
            {
                stepTimer = 0f;
                _wasMoving = false;
                return;
            }

            float interval = IsCrouching ? crouchStepInterval : IsSprinting ? sprintStepInterval : walkStepInterval;
            float volume = IsCrouching ? crouchStepVolume : IsSprinting ? sprintStepVolume : walkStepVolume;
            
            if (!_wasMoving)
            {
                PlaySound(
                    _walkSound[Random.Range(0, _walkSound.Length)],
                    volume
                );

                _wasMoving = true;
                stepTimer = 0f;
                return;
            }
            
            stepTimer += dt;

            if (stepTimer >= interval)
            {
                stepTimer -= interval;

                PlaySound(
                    _walkSound[Random.Range(0, _walkSound.Length)],
                    volume
                );
            }
        }
        
        public void PlaySound(String clip, float volumeToPlay)
        {
            InvokeEvent(new PlaySoundEvent
            {
                soundName = clip,
                position = transform.position,
                volume = volumeToPlay,
                pitch = Random.Range(0.95f, 1.05f) 
            });
        }
        
        public void SendPersonalisation()
        {
            if (!IsOwner) return;
            SubmitPersonalisationRpc(PlayerLocalData.instance.PlayerColor,
                PlayerLocalData.instance.PlayerName);
        }
        
        [Rpc(SendTo.Server)]
        private void SubmitPersonalisationRpc(int colorId, string playerName)
        {
            _syncedColor = colorId;
            _syncedName = playerName;
            ReplicatePersonalisationRpc(colorId, playerName);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestPersonalisationRpc()
        {
            ReplicatePersonalisationRpc(_syncedColor, _syncedName);
        }
        
        [Rpc(SendTo.Everyone)]
        private void ReplicatePersonalisationRpc(int colorId, string playerName)
        {
            if (PlayerLocalData.instance == null)
                return;

            meshRenderer[0].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            pseudoText.text = playerName;
        }
    }
}