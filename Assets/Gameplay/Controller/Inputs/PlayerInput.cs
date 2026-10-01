using System;
using MyPrint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Controller
{
    public class PlayerInput : MonoBehaviour
    {
        private Player _playerInputs;
        
        public Action OnStartLeftInput;
        public Action OnStartRightInput;
        public Action OnEndLeftInput;
        public Action OnEndRightInput;

        public Action OnJumpInput;
        public Action OnJumpInputCanceled;

        public Action OnSprintInput;
        public Action OnSprintInputCanceled;

        public Action OnCrouchInput;
        public Action OnCrouchInputCanceled;
        
        public Action OnInteractInput;

        public  Action OnLeanLeftInput;
        public  Action OnLeanLeftInputCanceled;
        public  Action OnLeanRightInput;
        public  Action OnLeanRightInputCanceled;

        public Action<Vector2> OnMoveInput;
        public Action<Vector2> OnMouseMovement;

        private void Awake()
        {
            _playerInputs = new Player();
        }

        private void OnEnable()
        {
            _playerInputs.Enable();
            
            _playerInputs.Gameplay.LeftClick.performed += PerformLeftClick;
            _playerInputs.Gameplay.RightClick.performed += PerformRightClick;
            _playerInputs.Gameplay.LeftRelease.performed += PerformLeftRelease;
            _playerInputs.Gameplay.RightRelease.performed += PerformRightRelease;
            
            _playerInputs.Gameplay.Movement.performed += PerformMovement;
            _playerInputs.Gameplay.Movement.canceled += PerformMovement;
            
            _playerInputs.Gameplay.Jump.performed += PerformJump;
            _playerInputs.Gameplay.Jump.canceled += PerformJumpCanceled;
            
            _playerInputs.Gameplay.Sprint.performed += PerformSprint;
            _playerInputs.Gameplay.Sprint.canceled += PerformSprintCanceled;
            
            _playerInputs.Gameplay.Crouch.performed += PerformCrouch;
            _playerInputs.Gameplay.Crouch.canceled += PerformCrouchCanceled;
            
            _playerInputs.Gameplay.Interact.performed += PerformInteract;
            
            _playerInputs.Gameplay.RightLean.performed += PerformRightLean;
            _playerInputs.Gameplay.RightLean.canceled += PerformRightLeanCanceled;
            _playerInputs.Gameplay.LeftLean.performed += PerformLeftLean;
            _playerInputs.Gameplay.LeftLean.canceled += PerformLeftLeanCanceled;
            
            _playerInputs.Gameplay.MouseMovement.performed += PerformeMouseMovement;
            _playerInputs.Gameplay.MouseMovement.canceled += PerformeMouseMovement;
        }

        private void OnDisable()
        {
            _playerInputs.Gameplay.LeftClick.performed -= PerformLeftClick;
            _playerInputs.Gameplay.RightClick.performed -= PerformRightClick;
            _playerInputs.Gameplay.LeftRelease.performed -= PerformLeftRelease;
            _playerInputs.Gameplay.RightRelease.performed -= PerformRightRelease;
            
            _playerInputs.Gameplay.Movement.performed -= PerformMovement;
            _playerInputs.Gameplay.Movement.canceled -= PerformMovement;
            
            _playerInputs.Gameplay.Jump.performed -= PerformJump;
            _playerInputs.Gameplay.Jump.canceled -= PerformJumpCanceled;
            
            _playerInputs.Gameplay.Sprint.performed -= PerformSprint;
            _playerInputs.Gameplay.Sprint.canceled -= PerformSprintCanceled;
            
            _playerInputs.Gameplay.Crouch.performed -= PerformCrouch;
            _playerInputs.Gameplay.Crouch.canceled -= PerformCrouchCanceled;
            
            _playerInputs.Gameplay.Interact.performed -= PerformInteract;
            
            _playerInputs.Gameplay.MouseMovement.performed -= PerformeMouseMovement;
            _playerInputs.Gameplay.MouseMovement.canceled -= PerformeMouseMovement;

            _playerInputs.Disable();
        }

        private void PerformLeftClick(InputAction.CallbackContext context)
        {
            OnStartLeftInput?.Invoke();
        }

        private void PerformRightClick(InputAction.CallbackContext context)
        {
            OnStartRightInput?.Invoke();
        }

        private void PerformLeftRelease(InputAction.CallbackContext context)
        {
            OnEndLeftInput?.Invoke();
        }

        private void PerformRightRelease(InputAction.CallbackContext context)
        {
            OnEndRightInput?.Invoke();
        }

        private void PerformMovement(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();

            OnMoveInput?.Invoke(movement);
        }

        private void PerformJump(InputAction.CallbackContext context)
        {
            OnJumpInput?.Invoke();
        }

        private void PerformJumpCanceled(InputAction.CallbackContext context)
        {
            OnJumpInputCanceled?.Invoke();
        }

        private void PerformSprint(InputAction.CallbackContext context)
        {
            OnSprintInput?.Invoke();
        }

        private void PerformSprintCanceled(InputAction.CallbackContext context)
        {
            OnSprintInputCanceled?.Invoke();
        }

        private void PerformCrouch(InputAction.CallbackContext context)
        {
            OnCrouchInput?.Invoke();
        }

        private void PerformCrouchCanceled(InputAction.CallbackContext context)
        {
            OnCrouchInputCanceled?.Invoke();
        }

        private void PerformInteract(InputAction.CallbackContext context)
        {
            OnInteractInput?.Invoke();
        }
        
        private void PerformeMouseMovement(InputAction.CallbackContext context)
        {
            OnMouseMovement?.Invoke(context.ReadValue<Vector2>());
        }

        private void PerformRightLean(InputAction.CallbackContext context)
        {
            OnLeanRightInput?.Invoke();
        }

        private void PerformRightLeanCanceled(InputAction.CallbackContext context)
        {
            OnLeanRightInputCanceled?.Invoke();
        }

        private void PerformLeftLean(InputAction.CallbackContext context)
        {
            OnLeanLeftInput?.Invoke();
        }

        private void PerformLeftLeanCanceled(InputAction.CallbackContext context)
        {
            OnLeanLeftInputCanceled?.Invoke();
        }
        
        private void OnDestroy() => _playerInputs.Dispose();
    }
}