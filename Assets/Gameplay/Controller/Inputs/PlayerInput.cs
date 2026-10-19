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
        public Action OnInteractInputCanceled;
        public Action OnDropInput;
        public Action OnDropInputCanceled;
        
        public  Action OnLeanInput;
        public  Action OnLeanInputCanceled;

        public Action<int> OnMouseRoll;

        public Action<Vector2> OnMoveInput;
        public Action<Vector2> OnMouseMovement;

        public Action OnPinging;

        public Action OnTryDropEquipment;
        
        public Action OnPause;

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
            _playerInputs.Gameplay.Interact.canceled += PerformInteractCanceled;

            _playerInputs.Gameplay.Drop.performed += PerformDrop;
            _playerInputs.Gameplay.Drop.canceled += PerformCanceled;
            
            _playerInputs.Gameplay.Lean.performed += PerformLean;
            _playerInputs.Gameplay.Lean.canceled += PerformLeanCanceled;
            
            _playerInputs.Gameplay.MouseMovement.performed += PerformeMouseMovement;
            _playerInputs.Gameplay.MouseMovement.canceled += PerformeMouseMovement;

            _playerInputs.Gameplay.MouseRoll.performed += PerformMouseRoll;

            _playerInputs.Gameplay.Ping.performed += PerformPing;

            _playerInputs.Gameplay.EquimentDrop.performed += PerformDropEquipment;
            
            _playerInputs.Gameplay.Pause.performed += PerformPause;
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
            
            _playerInputs.Gameplay.MouseRoll.performed -= PerformMouseRoll;
            
            _playerInputs.Gameplay.Ping.performed -= PerformPing;
            
            _playerInputs.Gameplay.EquimentDrop.performed -= PerformDropEquipment;

            _playerInputs.Gameplay.Pause.performed -= PerformPause;
            
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

        private void PerformInteractCanceled(InputAction.CallbackContext context)
        {
            OnInteractInputCanceled?.Invoke();
        }
        
        private void PerformDrop(InputAction.CallbackContext obj)
        {
            OnDropInput?.Invoke();
        }
        
        private void PerformCanceled(InputAction.CallbackContext obj)
        {
            OnDropInputCanceled?.Invoke();
        }
        
        private void PerformeMouseMovement(InputAction.CallbackContext context)
        {
            OnMouseMovement?.Invoke(context.ReadValue<Vector2>());
        }

        private void PerformLean(InputAction.CallbackContext context)
        {
            OnLeanInput?.Invoke();
        }

        private void PerformLeanCanceled(InputAction.CallbackContext context)
        {
            OnLeanInputCanceled?.Invoke();
        }
        
        private void PerformMouseRoll(InputAction.CallbackContext obj)
        {
            float value = obj.ReadValue<float>();

            if (value > 0)
            {
                OnMouseRoll?.Invoke(-1);
            }
            else
            {
                OnMouseRoll?.Invoke(1);
            }
        }
        
        private void PerformPing(InputAction.CallbackContext obj)
        {
            OnPinging?.Invoke();
        }
        
        private void PerformDropEquipment(InputAction.CallbackContext obj)
        {
            OnTryDropEquipment?.Invoke();
        }
        
        private void PerformPause(InputAction.CallbackContext obj)
        {
            OnPause?.Invoke();
        }
        
        private void OnDestroy() => _playerInputs.Dispose();
    }
}