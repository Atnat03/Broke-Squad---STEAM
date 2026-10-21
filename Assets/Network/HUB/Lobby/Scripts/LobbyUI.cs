using System;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MyPrint;
using System.Collections.Generic;
using Network.Connections;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Network.HUB
{
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
        [SerializeField] private PlayerElementVisual _playerVisualPrefab;
        [SerializeField] private PlayerVisualLobbySpawner _playerVisualSpawner;
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        
        private readonly List<PlayerElementVisual> _elementPlayerList = new();
        private readonly List<PlayerDataLobby> _dataPlayerList = new();

        [Header("Scene List UI")] 
        [SerializeField] private GameObject[] _onlyHostUI;
        [SerializeField] private SceneLobbyElement _sceneListPrefab;
        [SerializeField] private Transform _parentListScene;
        [SerializeField] private TextMeshProUGUI _selectedSceneName;
        
        [SerializeField] private Button _startGameButton;
        [SerializeField] private GameObject _loadingScreen;
        
        [Header("In Lobby Scene")]
        [SerializeField] private GameObject _inLobbyScene;
        [SerializeField] private Camera _firstCamera;
        
        private void OnEnable()
        {
            _lobbyManager.OnJoinLobby += JoinLobbyUI;
            _lobbyManager.OnUpdateJoinedLobby += UpdateLobbyList;
            _lobbyManager.OnUpdateLobbyInfo += UpdateUI;
            _lobbyManager.OnUpdatedMap += UpdateSelectedMapName;
            _lobbyManager.OnTryToJoinGame += ActivateLoadingScreen;
            _lobbyManager.OnLeaveLobby += LeaveLobbyUI;
        }
        
        private void OnDisable()
        {
            _lobbyManager.OnJoinLobby -= JoinLobbyUI;
            _lobbyManager.OnUpdateJoinedLobby -= UpdateLobbyList;
            _lobbyManager.OnUpdateLobbyInfo -= UpdateUI;
            _lobbyManager.OnUpdatedMap -= UpdateSelectedMapName;
            _lobbyManager.OnTryToJoinGame -= ActivateLoadingScreen; 
            _lobbyManager.OnLeaveLobby -= LeaveLobbyUI; 
            
            _elementList.Clear();
        }

        private void Start()
        {
            _firstCamera.gameObject.SetActive(true);
            _inLobbyScene.gameObject.SetActive(false);
            
            _inLobbyCanva.SetActive(false);
            _creationCanva.SetActive(true);

            int n = SceneManager.sceneCountInBuildSettings;
        
            for (int i = 1; i < n; i++)
            {
                SceneLobbyElement item = Instantiate(_sceneListPrefab, _parentListScene);
            
                item.SetScene(i, _lobbyManager);
            }
            
            _loadingScreen.SetActive(false);
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
            
            _inLobbyScene.gameObject.SetActive(true);
            _firstCamera.gameObject.SetActive(false);

            foreach (GameObject g in _onlyHostUI)
            {
                g.SetActive(isHost);
            }
            
            ABPrint.Print("Join Lobby : " +  lobby.Name, ABColor.Yellow);
        }
        
        private void LeaveLobbyUI()
        {
            foreach (PlayerElementVisual p in _elementPlayerList) Destroy(p.gameObject);
            
            _elementPlayerList.Clear();
            _dataPlayerList.Clear();

            _lobbyNameText.text = string.Empty;
            _loadingScreen.SetActive(false);

            _inLobbyCanva.SetActive(false);
            _creationCanva.SetActive(true);
            
            _inLobbyScene.gameObject.SetActive(false);
            _firstCamera.gameObject.SetActive(true);
        }
        
        private void UpdateUI(List<Unity.Services.Lobbies.Models.Player> list)
        {
            if (list.Count > _dataPlayerList.Count)
            {
                AddPlayer(list.Count - _dataPlayerList.Count);
            }

            if (list.Count < _dataPlayerList.Count)
            {
                for (int i = _dataPlayerList.Count - 1; i >= list.Count; i--)
                {
                    Destroy(_elementPlayerList[i].gameObject);
                    _elementPlayerList.RemoveAt(i);
                    _dataPlayerList.RemoveAt(i);
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                Unity.Services.Lobbies.Models.Player player = list[i];

                string nom = "Unknown";

                if (player.Data.TryGetValue("PlayerName", out PlayerDataObject valueName))
                    nom = valueName.Value;
                
                int colorId = 0;
			
                if(player.Data.TryGetValue("ColorId", out PlayerDataObject valueSkin))
                    int.TryParse(valueSkin.Value, out colorId);
			
                bool isReady = false;

                if(player.Data.TryGetValue("IsReady", out PlayerDataObject valueReady))
                    isReady = valueReady.Value == "1";

                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    //SetReady(isReady);
                }
			
                if (_dataPlayerList[i].Name != nom || _dataPlayerList[i].ColorID != colorId || _dataPlayerList[i].IsReady != isReady)
                {
                    _dataPlayerList[i] = new PlayerDataLobby
                    {
                        Name = nom,
                        ColorID = colorId,
                        IsReady = isReady
                    };

                    _elementPlayerList[i].SetData(_dataPlayerList[i]);
                }
            }
        }
        
        private void AddPlayer(int delta)
        {
            for (int i = 0; i < delta; i++)
            {
                int index = _elementPlayerList.Count;

                Transform t = _playerVisualSpawner.GetSpawnPoint(index);
                PlayerElementVisual ui = Instantiate(_playerVisualPrefab, t.position, t.rotation);
                _elementPlayerList.Add(ui);

                _dataPlayerList.Add(new PlayerDataLobby
                {
                    Name = "",
                    ColorID = 0,
                    IsReady = false
                });
            }
        }

        private void SwitchCanva()
        {
            _inLobbyCanva.SetActive(!_inLobbyCanva.activeSelf);
            _creationCanva.SetActive(!_creationCanva.activeSelf);
        }
        
        private void UpdateSelectedMapName(string newName)
        {
            _selectedSceneName.text = newName;
        }
        
        public void ActivateLoadingScreen()
        {
            _loadingScreen.SetActive(true);
        }

        private void Update()
        {
            _startGameButton.interactable = _lobbyManager.CheckAllPlayerReady();
        }
    }
}