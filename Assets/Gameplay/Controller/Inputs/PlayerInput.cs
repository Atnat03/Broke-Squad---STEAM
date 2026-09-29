using System;
using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Controller
{
    public class PlayerInput : MonoBusListener
    {
        Player _playerInputs;
        
        //Actions
        public Action OnStartLeftInput;
        public Action OnStartRightInput;
        public Action OnEndLeftInput;
        public Action OnEndRightInput;
        
        private void Awake() => _playerInputs = new Player();

        private void OnEnable()
        {
            _playerInputs.Enable();

            _playerInputs.Gameplay.LeftClick.performed += PerformLeftClick;
            _playerInputs.Gameplay.RightClick.performed += PerformRightClick;
            _playerInputs.Gameplay.LeftRelease.performed += PerformLeftRelease;
            _playerInputs.Gameplay.RightRelease.performed += PerformRightRelease;
        }
        
        private void PerformLeftClick(InputAction.CallbackContext obj)
        {
            OnStartLeftInput?.Invoke();
        }
        
        private void PerformRightClick(InputAction.CallbackContext obj)
        {
            OnStartRightInput?.Invoke();
        }

        private void PerformLeftRelease(InputAction.CallbackContext obj)
        {
            OnEndLeftInput?.Invoke();
        }

        private void PerformRightRelease(InputAction.CallbackContext obj)
        {
            OnEndRightInput?.Invoke();
        }
    }
}
