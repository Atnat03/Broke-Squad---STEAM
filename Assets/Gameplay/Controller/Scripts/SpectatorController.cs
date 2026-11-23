using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Controller
{
    public class SpectatorController : MonoBehaviour
    {
        private enum Mode { Free, FirstPerson, Orbit }

        [SerializeField] private Camera _cam;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _sprintMultiplier = 3f;
        [SerializeField] private float _lookSensitivity = 0.1f;
        [SerializeField] private float _orbitDistance = 4f;
        [SerializeField] private float _eyeHeight = 1.6f;

        private readonly List<PlayerController> _players = new();
        private int _index;
        private Mode _mode = Mode.Free;
        private float _yaw, _pitch;
        private float _refreshTimer;
        private bool _cursorFree;
        private PlayerController _hiddenTarget;
        
        private Vector2 _move;
        private Vector2 _lookDelta;
        private bool _up;
        private bool _down;
        private bool _sprint;

        private PlayerController Target => _players.Count > 0 ? _players[Mathf.Clamp(_index, 0, _players.Count - 1)] : null;

        private void Awake()
        {
            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _playerInput.OnMoveInput += OnMove;
            _playerInput.OnMouseMovement += OnLook;
            _playerInput.OnJumpInput += OnUpDown;
            _playerInput.OnJumpInputCanceled += OnUpUp;
            _playerInput.OnCrouchInput += OnDownDown;
            _playerInput.OnCrouchInputCanceled += OnDownUp;
            _playerInput.OnSprintInput += OnSprintDown;
            _playerInput.OnSprintInputCanceled += OnSprintUp;

            _playerInput.OnStartLeftInput += NextPlayer;      
            _playerInput.OnStartRightInput += PreviousPlayer;  
            _playerInput.OnMouseRoll += OnRoll;               
            _playerInput.OnInteractInput += CycleMode;         
            _playerInput.OnPause += ToggleCursor;             
        }

        private void OnDisable()
        {
            _playerInput.OnMoveInput -= OnMove;
            _playerInput.OnMouseMovement -= OnLook;
            _playerInput.OnJumpInput -= OnUpDown;
            _playerInput.OnJumpInputCanceled -= OnUpUp;
            _playerInput.OnCrouchInput -= OnDownDown;
            _playerInput.OnCrouchInputCanceled -= OnDownUp;
            _playerInput.OnSprintInput -= OnSprintDown;
            _playerInput.OnSprintInputCanceled -= OnSprintUp;

            _playerInput.OnStartLeftInput -= NextPlayer;
            _playerInput.OnStartRightInput -= PreviousPlayer;
            _playerInput.OnMouseRoll -= OnRoll;
            _playerInput.OnInteractInput -= CycleMode;
            _playerInput.OnPause -= ToggleCursor;
        }

        private void Start() => LockCursor(true);

        private void OnDestroy() { RestoreTargetMesh(); LockCursor(false); }

        #region Inputs

        private void OnMove(Vector2 v) => _move = v;
        private void OnLook(Vector2 v) => _lookDelta = v;
        private void OnUpDown() => _up = true;
        private void OnUpUp() => _up = false;
        private void OnDownDown() => _down = true;
        private void OnDownUp() => _down = false;
        private void OnSprintDown() => _sprint = true;
        private void OnSprintUp() => _sprint = false;

        private void NextPlayer()
        {
            if (!_cursorFree) Cycle(1);
        }

        private void PreviousPlayer()
        {
            if (!_cursorFree) Cycle(-1);
        }

        private void OnRoll(int dir)
        {
            if (!_cursorFree) Cycle(dir);
        }
        private void CycleMode() => SetMode((Mode)(((int)_mode + 1) % 3));
        private void ToggleCursor() => LockCursor(_cursorFree);

        #endregion

        private void Update()
        {
            RefreshPlayers();

            if (!_cursorFree)
            {
                Vector2 d = _lookDelta * _lookSensitivity;
                _yaw += d.x;
                _pitch = Mathf.Clamp(_pitch - d.y, -89f, 89f);
            }
            _lookDelta = Vector2.zero; 

            if (_mode == Mode.Free) FreeMove();
        }

        private void LateUpdate()
        {
            PlayerController t = Target;
            if (_mode == Mode.Free || t == null)
            {
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                return;
            }

            Vector3 eye = t.transform.position + Vector3.up * _eyeHeight;

            if (_mode == Mode.FirstPerson)
            {
                transform.SetPositionAndRotation(eye, Quaternion.Euler(_pitch, t.transform.eulerAngles.y, 0f));
            }
            else
            {
                Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
                transform.SetPositionAndRotation(eye - rot * Vector3.forward * _orbitDistance, rot);
            }
        }

        private void FreeMove()
        {
            Vector3 dir = transform.forward * _move.y + transform.right * _move.x + Vector3.up * ((_up ? 1 : 0) - (_down ? 1 : 0));

            float speed = _moveSpeed * (_sprint ? _sprintMultiplier : 1f);
            transform.position += Vector3.ClampMagnitude(dir, 1f) * speed * Time.unscaledDeltaTime;
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;
            if (mode != Mode.Free && Target != null)
            {
                _yaw = Target.transform.eulerAngles.y;
                _pitch = 10f;
            }
            UpdateTargetMesh();
        }

        private void Cycle(int step)
        {
            if (_players.Count == 0) return;
            _index = (_index + step + _players.Count) % _players.Count;
            if (_mode == Mode.FirstPerson) UpdateTargetMesh();
        }

        private void RefreshPlayers()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = 1f;

            PlayerController current = Target;
            _players.Clear();
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.IsSpawned) _players.Add(p);
            _players.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

            int i = current != null ? _players.IndexOf(current) : -1;
            _index = i >= 0 ? i : 0;
        }

        private void UpdateTargetMesh()
        {
            RestoreTargetMesh();
            if (_mode != Mode.FirstPerson || Target == null) return;

            _hiddenTarget = Target;
            foreach (var r in _hiddenTarget.meshRenderer)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        private void RestoreTargetMesh()
        {
            if (_hiddenTarget == null) return;
            foreach (var r in _hiddenTarget.meshRenderer)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _hiddenTarget = null;
        }

        private void LockCursor(bool locked)
        {
            _cursorFree = !locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}