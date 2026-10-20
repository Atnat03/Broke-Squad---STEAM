using System;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerInputReader : IDisposable
    {
        private readonly PlayerInput _playerInput;

        private Vector2 _move;
        private bool _sprintHeld, _crouchHeld, _leftLeanHeld, _rightLeanHeld;
        private bool _enabled = true;
        
        public Vector2 Move => _enabled ? _move : Vector2.zero;
        public bool SprintHeld => _enabled && _sprintHeld;
        public bool CrouchHeld => _enabled && _crouchHeld;
        public bool LeftLeanHeld => _enabled && _leftLeanHeld;
        public bool RightLeanHeld => _enabled && _rightLeanHeld;
        public float Lean => (RightLeanHeld ? 1f : 0f) - (LeftLeanHeld ? 1f : 0f);

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                OnEnabledChanged?.Invoke(value);
            }
        }

        public Action OnJumpPressed;
        public event Action<bool> OnEnabledChanged;

        public PlayerInputReader(PlayerInput playerInput)
        {
            _playerInput = playerInput;

            _playerInput.OnMoveInput += OnMove;
            _playerInput.OnJumpInput += OnJump;
            _playerInput.OnCrouchInput += OnCrouch;
            _playerInput.OnCrouchInputCanceled += OnCrouchCanceled;
            _playerInput.OnSprintInput += OnSprint;
            _playerInput.OnSprintInputCanceled += OnSprintCanceled;
        }

        private void OnMove(Vector2 v) => _move = v;
        private void OnJump() { if (_enabled) OnJumpPressed?.Invoke(); }
        private void OnCrouch() => _crouchHeld = true;
        private void OnCrouchCanceled() => _crouchHeld = false;
        private void OnSprint() => _sprintHeld = true;
        private void OnSprintCanceled() => _sprintHeld = false;
        private void OnLeanRight() => _rightLeanHeld = true;
        private void OnLeanRightCanceled() => _rightLeanHeld = false;
        private void OnLeanLeft() => _leftLeanHeld = true;
        private void OnLeanLeftCanceled() => _leftLeanHeld = false;

        public void Dispose()
        {
            _playerInput.OnMoveInput -= OnMove;
            _playerInput.OnJumpInput -= OnJump;
            _playerInput.OnCrouchInput -= OnCrouch;
            _playerInput.OnCrouchInputCanceled -= OnCrouchCanceled;
            _playerInput.OnSprintInput -= OnSprint;
            _playerInput.OnSprintInputCanceled -= OnSprintCanceled;
        }
    }
}