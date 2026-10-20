using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace Network.HUB
{
    public class LobbyUserInteraction : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobbyManager;
        
        [Header("Buttons")]
        [SerializeField] private Button _createLobbyButton;
        [SerializeField] private Button _leaveLobbyButton;
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField _lobbyNameInputField;

        void Awake()
        {
            _createLobbyButton.onClick.AddListener(() => _lobbyManager.TryCreateLobby(_lobbyNameInputField.text));
            _leaveLobbyButton.onClick.AddListener(() => _lobbyManager.LeaveCurrentLobby());
        }
    }
}