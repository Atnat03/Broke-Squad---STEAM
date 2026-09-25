using TMPro;
using UnityEngine.UI;

namespace Network.HUB
{
    using System.Collections.Generic;
    using MyPrint;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;

    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobbyManager;

        [Header("Canvas")] 
        [SerializeField] private GameObject _creationCanva;
        [SerializeField] private GameObject _inLobbyCanva;

        [Header("Lobby List UI")]
        [SerializeField] private LobbyListElement _lobbyListElementPrefab;
        [SerializeField] private Transform _parentList;
        private readonly List<LobbyListElement> _elementList = new();
        
        [Header("In Lobby UI")]
        [SerializeField] private GameObject _playerNotifPrefab;
        [SerializeField] private Transform _playerNotifParent;
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        private readonly List<GameObject> _elementPlayerList = new();
        
        [SerializeField] private Button _startGameButton;
        
        private void OnEnable()
        {
            _lobbyManager.OnJoinLobby += JoinLobbyUI;
            _lobbyManager.OnUpdateJoinedLobby += UpdateLobbyList;
            _lobbyManager.OnUpdateLobbyInfo += UpdateLobbyInfo;
        }

        private void OnDisable()
        {
            _lobbyManager.OnJoinLobby -= JoinLobbyUI;
            _lobbyManager.OnUpdateJoinedLobby -= UpdateLobbyList;
            _lobbyManager.OnUpdateLobbyInfo -= UpdateLobbyInfo;
            
            _elementList.Clear();
        }

        private void Start()
        {
            _inLobbyCanva.SetActive(false);
            _creationCanva.SetActive(true);
        }

        private void UpdateLobbyList(List<Lobby> list)
        {
            while (_elementList.Count < list.Count)
            {
                LobbyListElement element = Instantiate(_lobbyListElementPrefab, _parentList);

                _elementList.Add(element);
            }

            for (int i = 0; i < list.Count; i++)
            {
                _elementList[i].gameObject.SetActive(true);
                _elementList[i].SetElement(_lobbyManager, list[i]);
            }

            for (int i = list.Count; i < _elementList.Count; i++)
            {
                _elementList[i].gameObject.SetActive(false);
            }
        }

        private void JoinLobbyUI(Lobby lobby, bool isHost)
        {
            SwitchCanva();
            
            _startGameButton.gameObject.SetActive(isHost);
            
            ABPrint.Print("Join Lobby : " +  lobby.Name, ABColor.Yellow);
        }
        
        private void UpdateLobbyInfo(Lobby lobby)
        {
            var list = lobby.Players;
            
            while (_elementPlayerList.Count < list.Count)
            {
                GameObject newPlayer = Instantiate(_playerNotifPrefab, _playerNotifParent);
                _elementPlayerList.Add(newPlayer);
            }

            for (int i = list.Count; i < _elementList.Count; i++)
            {
                _elementList[i].gameObject.SetActive(false);
            }
            
            if(!_lobbyNameText.text.Equals(lobby.Name))
                _lobbyNameText.text = lobby.Name;
        }

        private void SwitchCanva()
        {
            _inLobbyCanva.SetActive(!_inLobbyCanva.activeSelf);
            _creationCanva.SetActive(!_creationCanva.activeSelf);
        }
    }
}

