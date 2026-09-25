namespace Network.HUB
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Unity.Services.Lobbies.Models;
    
    public class LobbyListElement : MonoBehaviour
    {
        [SerializeField] private Button _joinButton;
        [SerializeField] private TextMeshProUGUI _lobbyName;
        [SerializeField] private TextMeshProUGUI _lobbyNumberPlayer;

        LobbyManager _lobbyManager;

        public void SetElement(LobbyManager lobbyManager, Lobby lobby)
        {
            _lobbyManager = lobbyManager;
            
            UpdateLobby(lobby);
        }
        
        void UpdateLobby(Lobby lobby)
        {
            _lobbyName.text = lobby.Name;
            _lobbyNumberPlayer.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

            if (_lobbyManager == null) 
                return;
            
            _joinButton.onClick.RemoveAllListeners();
            _joinButton.onClick.AddListener(() => _lobbyManager.JoinById(lobby.Id));
        }
    }
}

