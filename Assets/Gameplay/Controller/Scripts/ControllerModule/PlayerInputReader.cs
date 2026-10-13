using System;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerInputReader : IDisposable
    {
        private readonly PlayerInput _playerInput;
        
        public Vector2 Move {get ; private set;}
        public bool SprintHeld {get ; private set;}
        public bool CrouchHeld {get ; private set;}
        public bool LeftLeanHeld {get ; private set;}
        public bool RightLeanHeld {get ; private set;}
        public float Lean => (RightLeanHeld ? 1f : 0f) - (LeftLeanHeld ? 1f : 0f);

        public Action OnJumpPressed;

        public PlayerInputReader(PlayerInput playerInput)
        {
            _playerInput = playerInput;
            
            _playerInput.OnMoveInput += OnMove;
            _playerInput.OnJumpInput += OnJump;
            _playerInput.OnCrouchInput += OnCrouch;
            _playerInput.OnCrouchInputCanceled += OnCrouchCanceled;
            _playerInput.OnSprintInput += OnSprint;
            _playerInput.OnSprintInputCanceled += OnSprintCanceled;
            _playerInput.OnLeanRightInput += OnLeanRight;
            _playerInput.OnLeanRightInputCanceled += OnLeanRightCanceled;
            _playerInput.OnLeanLeftInput += OnLeanLeft;
            _playerInput.OnLeanLeftInputCanceled += OnLeanLeftCanceled;
        }
        
        private void OnMove(Vector2 v) => Move = v;
        private void OnJump() => OnJumpPressed?.Invoke();
        private void OnCrouch() => CrouchHeld = true;
        private void OnCrouchCanceled() => CrouchHeld = false;
        private void OnSprint() => SprintHeld = true;
        private void OnSprintCanceled() => SprintHeld = false;
        private void OnLeanRight() => RightLeanHeld = true;
        private void OnLeanRightCanceled() => RightLeanHeld = false;
        private void OnLeanLeft() => LeftLeanHeld = true;
        private void OnLeanLeftCanceled() => LeftLeanHeld = false;
        
        public void Dispose()
        {
            _playerInput.OnMoveInput -= OnMove;
            _playerInput.OnJumpInput -= OnJump;
            _playerInput.OnCrouchInput -= OnCrouch;
            _playerInput.OnCrouchInputCanceled -= OnCrouchCanceled;
            _playerInput.OnSprintInput -= OnSprint;
            _playerInput.OnSprintInputCanceled -= OnSprintCanceled;
            _playerInput.OnLeanRightInput -= OnLeanRight;
            _playerInput.OnLeanRightInputCanceled -= OnLeanRightCanceled;
            _playerInput.OnLeanLeftInput -= OnLeanLeft;
            _playerInput.OnLeanLeftInputCanceled -= OnLeanLeftCanceled;
        }
    }
}