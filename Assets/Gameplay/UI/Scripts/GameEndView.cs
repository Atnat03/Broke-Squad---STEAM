using System.Collections;
using Bus;
using Gameplay.Other;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class GameEndView : MonoBusListener
    {
        [SerializeField] private GameObject _popup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _leaveButton;
        
        private bool _leaving;

        private void Awake()
        {
            _popup.SetActive(false);
            _leaveButton.onClick.AddListener(LeaveGame);
            ListenToEvent<OnGameEnded_EVENT>(ShowPopup);
        }

        private void OnEnable()
        {
            var networkManager = NetworkManager.Singleton;
            
            if (networkManager != null && networkManager.IsClient && !networkManager.IsServer) networkManager.OnClientDisconnectCallback += OnDisconnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }

        private void ShowPopup(OnGameEnded_EVENT data)
        {
            _titleText.text = data.Win ? "You win!" : "You lose";
            _popup.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) LeaveGame();
        }

        private void LeaveGame()
        {
            if (_leaving) return;
            _leaving = true;

            var networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= OnDisconnected;
                networkManager.Shutdown();
            }

            SceneManager.LoadScene("HUB");
        }
    }
    
    
}