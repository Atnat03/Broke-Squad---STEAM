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
        #region Variables
        [Header("Profile")]
        [SerializeField] private ControllerProfileSO profile;

        [Header("Modules")]
        [SerializeField] private GroundChecker ground;
        [SerializeField] private PlayerBody body;
        [SerializeField] private PlayerFootsteps footsteps;

        [Header("To Assign")]
        public PlayerCamera playerCamera;
        public GameObject UI;
        public MeshRenderer[] meshRenderer;
        public TextMeshProUGUI pseudoText;

        [Header("Tied Up")]
        [SerializeField] private float tiedUpSpeed = 1f;

        [Header("SFX")]
        [SerializeField, SoundName] private string _jumpSound;
        [SerializeField, SoundName] private string _crouchSound;
        
        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private PlayerInput _playerInput;
        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private PlayerStamina _stamina;
        private StateMachine _stateMachine;
        
        private int _syncedColor;
        private string _syncedName = "";

        private bool _playerDead;

        private readonly NetworkVariable<bool> _netCrouching = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _netDown = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        public bool IsGrounded => ground.IsGrounded;
        public bool IsCrouching => IsOwner ? _stateMachine?.CurrentState is CrouchState : _netCrouching.Value;
        public bool IsSprinting => IsOwner && _stateMachine?.CurrentState is SprintState;
        public bool IsDown      => IsOwner ? _stateMachine?.CurrentState is TiedUpState : _netDown.Value;
        
        public float MaxStamina => profile.maxStamina;
        public float LeanInput => _input?.Lean ?? 0f;
        public Vector3 HorizontalVelocity => _motor?.HorizontalVelocity ?? Vector3.zero;
        public float ForwardDot => _motor?.ForwardDot ?? 0f;
        public bool IsMovingForward => ForwardDot > 0.1f;
        public bool IsMovingBackward => ForwardDot < -0.1f;
        
        public ControllerProfileSO Profile => profile;
        public PlayerMotor Motor => _motor;
        public PlayerBody Body => body;
        public PlayerStamina Stamina => _stamina;
        public float TiedUpSpeed => tiedUpSpeed;
        
        private bool WantsCrouch() => _input.CrouchHeld || (IsCrouching && !body.CanStandUp());
        private bool WantsSprint() => _input.SprintHeld && _input.Move.y > 0.1f && _stamina.CanSprint;

        public void PlayCrouchSound() => PlaySound(_crouchSound, 0.5f);

        public void SetYaw(float yawDegrees) => _rb.MoveRotation(Quaternion.Euler(0f, yawDegrees, 0f));

        #endregion

        #region Initialization

        public override void OnNetworkSpawn()
        {
            if (profile == null)
            {
                Debug.LogError("PlayerController: profile is not assigned.", this);
                enabled = false;
                return;
            }

            SetUpComponents();

            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                UI.SetActive(false);
                RequestPersonalisationRpc();
                return;
            }

            foreach (var mesh in meshRenderer) mesh.enabled = false;
            pseudoText.gameObject.SetActive(false);

            SendPersonalisation();
            SetUpModules();
            SetUpStateMachine();
            ListenToEvent<PlayerDeathEvent>(SetPlayerDead);
        }

        public override void OnNetworkDespawn()
        {
            _input?.Dispose();
            _input = null;
            base.OnNetworkDespawn();
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

            body.Init(transform, _capsule, profile);
        }
        
        private void SetUpModules()
        {
            _playerInput = TryGetComponent(out PlayerInput existing) ? existing : gameObject.AddComponent<PlayerInput>();
            _input = new PlayerInputReader(_playerInput);

            _stamina = new PlayerStamina(profile, (current, max) =>
                InvokeEvent(new StaminaChangedEvent { stamina = current, maxStamina = max }));

            _motor = new PlayerMotor(_rb, transform, profile, ground, _input);
            _motor.OnJump += () => PlaySound(_jumpSound, 0.5f);
            _input.OnJumpPressed += _motor.BufferJump;

            footsteps.Init(PlaySound);
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();

            var movementState = new MovementState(this);
            var sprintState   = new SprintState(this);
            var crouchState   = new CrouchState(this);
            var tiedState     = new TiedUpState(this);

            Any(tiedState, new FuncPredicate(() => _playerDead));
            At(tiedState, movementState, new FuncPredicate(() => !_playerDead));
            
            At(movementState, crouchState,   new FuncPredicate(WantsCrouch));
            At(sprintState,   crouchState,   new FuncPredicate(WantsCrouch));
            At(crouchState,   movementState, new FuncPredicate(() => !WantsCrouch()));

            At(movementState, sprintState,   new FuncPredicate(WantsSprint));
            At(sprintState,   movementState, new FuncPredicate(() => !WantsSprint()));

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
                body.ApplyRemote(Time.deltaTime, _netCrouching.Value, _netDown.Value);
                return;
            }
            
            _stateMachine.Update();
            body.UpdateEye(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner) return;
            
            float dt = Time.fixedDeltaTime;
            ground.Check(body, _rb, transform);
            
            _stateMachine.FixedUpdate();       
            body.Tick(dt);
            _motor.FixedTick(dt);
            _stamina.Tick(dt);                    

            _netCrouching.Value = IsCrouching;
            _netDown.Value = IsDown;

            footsteps.Tick(dt, IsGrounded, _input.Move.sqrMagnitude > 0.01f, IsCrouching, IsSprinting);
        }

        private void LateUpdate() { if (IsOwner) _stateMachine.LateUpdate(); }

        
        

        #endregion

        #region Death / Sound

        private void SetPlayerDead(PlayerDeathEvent e)
        {
            if (e.playerID != OwnerClientId) return;
            _playerDead = true;
            _motor.StopMoving();
        }

        public void Revive() => _playerDead = false;

        public void PlaySound(string clip, float volumeToPlay)
        {
            InvokeEvent(new PlaySoundEvent
            {
                soundName = clip,
                position = transform.position,
                volume = volumeToPlay,
                pitch = Random.Range(0.95f, 1.05f)
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestPersonalisationRpc() => ReplicatePersonalisationRpc(_syncedColor, _syncedName);

        [Rpc(SendTo.Everyone)]
        private void ReplicatePersonalisationRpc(int colorId, string playerName)
        {
            if (PlayerLocalData.instance == null) return;
            meshRenderer[0].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            pseudoText.text = playerName;
        }
        

        #endregion
    }
}