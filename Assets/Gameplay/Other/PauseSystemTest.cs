using Bus;
using Gameplay.Controller;
using MyPrint;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


namespace Gameplay.Other
{
    public class PauseSystemTest :  MonoBusListener
    {
        [SerializeField] private PlayerSetUpBridge _bridge;
        
        [Header("UI")]
        [SerializeField] private GameObject _uiPause;

        private bool _isPause;
        
        public void Start()
        {
            _bridge.PlayerInput.OnPause += UpdatePause;
                
            _uiPause.gameObject.SetActive(false);
        }

        private void UpdatePause()
        {
            _isPause = !_isPause;
            
            _uiPause.gameObject.SetActive(_isPause);
            
            Cursor.visible = !_isPause;
            Cursor.lockState = !_isPause ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = _isPause;

            _bridge.PlayerController.SetInputEnabled(!_isPause);
        }

        public void ToggleFpsCounter(Toggle toggle)
        {
            Debug.Log("evt");
            InvokeEvent(new FPSCounterChangeVisual_EVENT{show = toggle.isOn});    
        }
    }
}