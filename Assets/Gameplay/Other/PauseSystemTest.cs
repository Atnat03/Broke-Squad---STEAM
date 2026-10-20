using Gameplay.Controller;
using MyPrint;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Other
{
    public class PauseSystemTest : NetworkBehaviour
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private PlayerController _playerController;
        
        [Header("UI")]
        [SerializeField] private GameObject _uiPause;

        private bool _isPause;
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _playerInput.OnPause += UpdatePause;
                
                _uiPause.gameObject.SetActive(false);
            }
        }

        private void UpdatePause()
        {
            _isPause = !_isPause;
            
            _uiPause.gameObject.SetActive(_isPause);
            
            Cursor.visible = !_isPause;
            Cursor.lockState = !_isPause ? CursorLockMode.Locked : CursorLockMode.None;

            _playerController.SetInputEnabled(!_isPause);
        }
    }
}