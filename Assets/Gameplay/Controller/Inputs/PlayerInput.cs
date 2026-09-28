using System;
using MyPrint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Controller
{
    public class PlayerInput : MonoBehaviour
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
            ABPrint.Print("Left click", ABColor.Red);
        }
        
        private void PerformRightClick(InputAction.CallbackContext obj)
        {
            OnStartRightInput?.Invoke();
            ABPrint.Print("Right click", ABColor.Red);
        }

        private void PerformLeftRelease(InputAction.CallbackContext obj)
        {
            OnEndLeftInput?.Invoke();
            ABPrint.Print("Left release", ABColor.Red);
        }

        private void PerformRightRelease(InputAction.CallbackContext obj)
        {
            OnEndRightInput?.Invoke();
            ABPrint.Print("Right release", ABColor.Red);
        }
    }
}
