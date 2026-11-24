using System;
using System.Collections;
using Bus;
using Gameplay.Controller.States;
using Gameplay.LD;
using Gameplay.Other.SuspiciousSound;
using Gameplay.PlayerData;
using MyPrint;
using Network.Connections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Controller
{
    public class PlayerController : NetworkBusListener
    {
        #region Variables

        [Header("Profile")]
        [SerializeField] private ControllerProfileSO profile;

        [Header("Modules")]
        [SerializeField] private GroundChecker ground;
        [SerializeField] private PlayerBody body;
        [SerializeField] private PlayerFootsteps footsteps;
        [SerializeField] private LadderChecker ladderChecker;
        [SerializeField] private PlayerAnimator playerAnimator;

        [Header("To Assign")]
        public PlayerCamera PlayerCamera;
        [SerializeField] private PlayerHealth health;
        public Renderer[] meshRenderer;

        [Header("Tied Up")]
        [SerializeField] private float tiedUpSpeed = 1f;

        [Header("Stun")]
        private float _stunTimer;
        public bool IsStunned => _stunTimer > 0f;
        
        [Header("SFX")]
        [SerializeField, SoundName] private string _jumpSound;
        [SerializeField, SoundName] private string _crouchSound;

        [Header("Components")]
        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private PlayerInput _playerInput;
        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private PlayerStamina _stamina;
        private StateMachine _stateMachine;
        
        private int _syncedColor;
        private string _syncedName = "";

        private readonly NetworkVariable<bool> _netCrouching = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private int _lockCount;
        public bool IsLocked => _lockCount > 0;
        
        private float _ladderCooldown;
        public PlayerHealth Health => health;
        public bool IsGrounded => ground.IsGrounded;
        public bool IsCrouching => IsOwner ? _stateMachine?.CurrentState is CrouchState : _netCrouching.Value;
        public bool IsSprinting => IsOwner && _stateMachine?.CurrentState is SprintState;
        public bool IsDown => health != null && health.IsDowned;

        public float MaxStamina => profile.maxStamina;
        public float LeanInput => _input?.Lean ?? 0f;
        public Vector3 HorizontalVelocity => _motor?.HorizontalVelocity ?? Vector3.zero;
        public float ForwardDot => _motor?.ForwardDot ?? 0f;
        public bool IsMovingForward => ForwardDot > 0.1f;
        public bool IsMovingBackward => ForwardDot < -0.1f;
        public bool InputEnabled => _input == null || _input.Enabled;

        public ControllerProfileSO Profile => profile;
        public PlayerMotor Motor => _motor;
        public PlayerBody Body => body;
        public PlayerStamina Stamina => _stamina;
        public LadderChecker LadderChecker => ladderChecker;
        public PlayerAnimator PlayerAnimator => playerAnimator;

        public PlayerHealth PlayerHealth => health;
        public float TiedUpSpeed => tiedUpSpeed;

        private bool WantsCrouch() => _input.CrouchHeld || (IsCrouching && !body.CanStandUp());
        private bool WantsSprint() => (_input.SprintHeld || ForcedSprint) && MoveInput.y > 0.1f && _stamina.CanSprint;

        public void PlayCrouchSound() => PlaySound(_crouchSound, 0.5f);

        public void SetYaw(float yawDegrees) => _rb.MoveRotation(Quaternion.Euler(0f, yawDegrees, 0f));
        
        public void PushLock() => _lockCount++;
        public void PopLock() => _lockCount = Mathf.Max(0, _lockCount - 1);

        private bool CanAct => !PlayerCamera.LocksController && !IsStunned && !IsLocked;
        public Vector2 MoveInput => _input != null && CanAct ? (ForcedMove ?? _input.Move) : Vector2.zero;

        
        
        public Vector2? ForcedMove { get; private set; }
        public bool ForcedSprint { get; private set; }

        public void SetForcedInput(Vector2? move, bool sprint)
        {
            ForcedMove = move;
            ForcedSprint = sprint;
            if (_motor != null) _motor.ForcedMove = move;
        }

        public Vector3 RawDesiredDirection
        {
            get
            {
                Vector2 m = _input != null ? _input.Move : Vector2.zero;
                return transform.TransformDirection(Vector3.ClampMagnitude(new Vector3(m.x, 0f, m.y), 1f));
            }
        }

        public void HoldAt(Vector3 pos)
        {
            Vector3 newPos = new Vector3(pos.x, transform.position.y, pos.z);
            _rb.MovePosition(newPos); 
            _rb.linearVelocity = Vector3.zero;
        }
        
        public void StartLadderCooldown() => _ladderCooldown = Profile.ladderRegrabCooldown;
        
        private bool WantsClimb() => _ladderCooldown <= 0f && ladderChecker.Touching;

        public float ClimbInput => ladderChecker.GetClimbInput(_motor.DesiredDirection);

        private bool KeepClimbing() =>
            (ladderChecker.Touching || ladderChecker.HeadTouching)
            && !(IsGrounded && ClimbInput < 0f)
            && !_ladderJumpRequested;
        
        private bool _ladderJumpRequested;
        public bool LadderJumpRequested => _ladderJumpRequested;
        public void RequestLadderJump() => _ladderJumpRequested = true;
        public void ClearLadderJump() => _ladderJumpRequested = false;
        
        public bool NoclipActive { get; set; }
        public bool CrouchHeld => _input != null && _input.CrouchHeld;
        public bool SprintHeld => _input != null && _input.SprintHeld;

        public string PlayerName { get; private set; }
        public int PlayerColorId { get; private set; }
        
        public Action OnPersonalisationChanged;
        public event Action JumpPressed;
        public Action<string> OnPseudoIsSetUp;
        
        #endregion

        #region Initialization

        public override void OnNetworkSpawn()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
            if (profile == null || health == null)
            {
                Debug.LogError("PlayerController: profile is not assigned.", this);
                enabled = false;
                return;
            }

            SetUpComponents();

            if (!IsOwner)
            {
                PlayerCamera.gameObject.SetActive(false);
                RequestPersonalisationRpc();
                return;
            }

            foreach (var mesh in meshRenderer) mesh.gameObject.layer = LayerMask.NameToLayer("HideToOwner");

            SendPersonalisation();
            SetUpModules();
            SetUpStateMachine();

        }

        public override void OnNetworkDespawn()
        {
            _stunTimer = 0f;
            _input?.Dispose();
            _input = null;
            base.OnNetworkDespawn();
        }

        private void SetUpComponents()
        {
            
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            if(!IsOwner)_rb.isKinematic = true;
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

            body.Init(transform, _capsule, profile);
        }

        private void SetUpModules()
        {
            _playerInput = GetComponent<PlayerInput>();
            _input = new PlayerInputReader(_playerInput);

            _stamina = new PlayerStamina(profile, (current, max) =>
                InvokeEvent(new StaminaChanged_EVENT { Current = current, MaxStamina = max }));

            _motor = new PlayerMotor(_rb, transform, profile, ground, _input, body);
            _motor.OnJump += () => PlaySound(_jumpSound, 0.5f);
            _motor.OnJump += () => PlayerAnimator.SetAnimTrigger("Jump");
            _input.OnJumpPressed += () => JumpPressed?.Invoke();
            _input.OnJumpPressed += _motor.BufferJump;

            footsteps.Init(PlaySound, this);
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();

            var movementState = new MovementState(this);
            var sprintState = new SprintState(this);
            var crouchState = new CrouchState(this);
            var tiedState = new TiedUpState(this);
            var climbState = new ClimbState(this);



            Any(tiedState, new FuncPredicate(() => health.IsDowned));
            At(tiedState, movementState, new FuncPredicate(() => !health.IsDowned && !body.CanStandUp()));
            At(tiedState, crouchState, new FuncPredicate(() => !health.IsDowned && body.CanStandUp()));
            At(movementState, crouchState, new FuncPredicate(WantsCrouch));
            At(sprintState, crouchState, new FuncPredicate(WantsCrouch));
            At(crouchState, movementState, new FuncPredicate(() => !WantsCrouch()));
            At(movementState, sprintState, new FuncPredicate(WantsSprint));
            At(sprintState, movementState, new FuncPredicate(() => !WantsSprint()));
            At(movementState, climbState, new FuncPredicate(WantsClimb));
            At(sprintState, climbState, new FuncPredicate(WantsClimb));
            At(climbState, movementState, new FuncPredicate(() => !KeepClimbing()));

            _stateMachine.SetState(movementState);
        }

        void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        #endregion

        #region Updates

        private void Update()
        {
            if (!IsSpawned) return;

            if (!IsOwner)
            {
                body.ApplyRemote(Time.deltaTime, _netCrouching.Value, IsDown);
                return;
            }
            
            if (NoclipActive) return;

            _stunTimer -= Time.deltaTime;
            _motor.CanMove = !PlayerCamera.LocksController && !IsStunned && CanAct;
            _stateMachine.Update();
            body.UpdateEye(Time.deltaTime);
            playerAnimator.UpdateState(
                crouched: IsCrouching,
                moving: HorizontalVelocity.sqrMagnitude > 0.1f && _input.Move.magnitude > 0.1f,
                sprinting: IsSprinting,
                downed: IsDown,
                grounded: IsGrounded);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner) return;
            if (NoclipActive) return;
            
            float dt = Time.fixedDeltaTime;
            ground.Check(body, _rb, transform);
            Vector3 probeDir = _stateMachine.CurrentState is ClimbState ? -ladderChecker.LadderNormal : _motor.DesiredDirection;
            ladderChecker.Check(body, probeDir);
            _ladderCooldown -= dt;
            _stateMachine.FixedUpdate();
            body.Tick(dt);
            _motor.FixedTick(dt);
            _stamina.Tick(dt);

            _netCrouching.Value = IsCrouching;

            footsteps.Tick(dt, IsGrounded, MoveInput.sqrMagnitude > 0.01f, IsCrouching, IsSprinting);
        }

        private void LateUpdate() { if (IsOwner) _stateMachine.LateUpdate(); }

        #endregion

        #region  Sound

        public void PlaySound(string clip, float volumeToPlay)
        {
            InvokeEvent(new PlaySoundEvent
            {
                SoundName = clip,
                Position = transform.position,
                Volume = volumeToPlay,
                Pitch = Random.Range(0.95f, 1.05f)
            });
        }

        #endregion

        #region Personalisation

        public void SendPersonalisation()
        {
            if (!IsOwner) return;
            SubmitPersonalisationRpc(PlayerLocalData.instance.PlayerColor, PlayerLocalData.instance.PlayerName);
        }

        [Rpc(SendTo.Server)]
        private void SubmitPersonalisationRpc(int colorId, string playerName)
        {
            _syncedColor = colorId;
            _syncedName = playerName;
            
            ReplicatePersonalisationRpc(colorId, playerName);
        }

        [Rpc(SendTo.Server)]
        private void RequestPersonalisationRpc() => ReplicatePersonalisationRpc(_syncedColor, _syncedName);

        [Rpc(SendTo.Everyone)]
        private void ReplicatePersonalisationRpc(int colorId, string playerName)
        {
            if (PlayerLocalData.instance == null) return;
            meshRenderer[0].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            meshRenderer[1].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            meshRenderer[2].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            meshRenderer[3].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            meshRenderer[4].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            OnPseudoIsSetUp?.Invoke(playerName);
            
            PlayerColorId = colorId;
            PlayerName = playerName;
            
            OnPersonalisationChanged?.Invoke();
        }

        [Rpc(SendTo.Server)]
        public void SendEventSoundServerRpc(float force)
        {
            InvokeEvent(new OnCreateSuspiciousSound_EVENT
            {
                FromClientID = OwnerClientId,
                Force = force,
                Position = transform.position,
            });
        }


        #endregion

        public void  SetInputEnabled(bool value)
        {
            if (!IsOwner || _input == null) return;
            _input.Enabled = value;

            if (!value) _motor?.StopMoving();
        }

        public void SnapTo(Vector3 position, float yawDegrees)
        {
            _rb.position = position;
            _rb.linearVelocity = Vector3.zero;
            SetYaw(yawDegrees);
        }

        [Rpc(SendTo.Owner)]
        public void TeleportClientRpc(Vector3 position, Quaternion rotation)
        {
            if(!IsOwner) return;
            _rb.linearVelocity = Vector3.zero;
            _rb.position = position;
            _rb.rotation = rotation;
        }

        public void Stun(float duration)
        {
            if (IsOwner) ApplyStun(duration);
            else StunOwnerRpc(duration);
        }

        [Rpc(SendTo.Owner)]
        private void StunOwnerRpc(float duration) => ApplyStun(duration);

        private void ApplyStun(float duration)
        {
            _stunTimer = Mathf.Max(_stunTimer, duration); 
            _motor?.StopMoving();                  
        }
    }
}