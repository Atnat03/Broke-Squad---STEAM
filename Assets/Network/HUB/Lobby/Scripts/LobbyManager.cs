using System.Threading.Tasks;
using MyPrint;
using Network.Connections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Network.HUB
{
    using System;
    using System.Collections.Generic;
    using Unity.Services.Authentication;
    using Unity.Services.Core;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public class LobbyManager : MonoBehaviour
    {
        public static int MAX_PLAYER_COUNT => MAX_PLAYER;
        const int MAX_PLAYER = 4;
        
        //Lobby var
        private Lobby _hostLobby;
        private Lobby _joinedLobby;
        
        //KeyDataName
        private string KEY_RELAY = "Relay";
        private string KEY_MAP = "Map";

        //Current time before update Lobbies
        private float _heartbeatTimer;
        private float _lobbyUpdateTimer;
        private float _lobbyListUpdateTimer;

        //Timer to update Lobbies 
        [SerializeField] private float _lobbyUpdateTimerMax = 2f;
        [SerializeField] private float _heartbeatTimerMax = 3f;
        [SerializeField] private float _lobbyListUpdateTimerMax = 1.5f;
        
        //In Lobby
        private string _sceneNameToPlayTogether;
        private string _selectedMap;

        //Other variables
        private string _playerName;
        private bool _relayJoined;

        //Actions
        public Action<Lobby, bool> OnJoinLobby;
        public Action<List<Lobby>> OnUpdateJoinedLobby;
        public Action<Lobby> OnUpdateLobbyInfo;
        public Action<string> OnUpdatedMap;
        public Action OnTryToJoinGame;

        private async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();
                
                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };
                
                AuthenticationService.Instance.SwitchProfile("player_" + Random.Range(0, 100000));

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                _playerName = "Player " + Random.Range(0, 99);
                
                string _sceneName = SceneUtility.GetScenePathByBuildIndex(1);
                _sceneNameToPlayTogether = System.IO.Path.GetFileNameWithoutExtension(_sceneName);
                _selectedMap = _sceneNameToPlayTogether;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        #region Useful Methos

        //Method to call from other script to join the lobby
        public void JoinById(string id) => JoinLobbyById(id);

        //Method to call from other script to create lobby
        public void TryCreateLobby(string lobbyName) => CreateLobby(lobbyName);

        #endregion

        #region Update Lobby

        private void Update()
        {
            HandleLobbyHeartbeat();
            HandleLobbyPollForUpdates();
            HandleUpdateLobbyList();
        }

        //Send a ping each x second to check if the lobby is still connected
        private async void HandleLobbyHeartbeat()
        {
            try
            {
                if (_hostLobby != null)
                {
                    _heartbeatTimer -= Time.deltaTime;
                    if (_heartbeatTimer < 0)
                    {
                        _heartbeatTimer = _heartbeatTimerMax;

                        await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        //Catch the new Lobby with the modification that was applies to it (synchronize update)
        private async void HandleLobbyPollForUpdates()
        {
            if (_joinedLobby == null) return;

            _lobbyUpdateTimer -= Time.deltaTime;
            if (_lobbyUpdateTimer >= 0) return;
            _lobbyUpdateTimer = _lobbyUpdateTimerMax;

            try
            {
                _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);

                OnUpdateLobbyInfo?.Invoke(_joinedLobby);
                
                if (IsLobbyHost() || _relayJoined) return;

                if (_joinedLobby.Data.TryGetValue(KEY_RELAY, out var relayData) && relayData.Value != "0")
                {
                    OnTryToJoinGame?.Invoke();
                    
                    _relayJoined = true;
                    _relayJoined = await RelayManager.instance.JoinRelay(relayData.Value);
                }
            }
            catch (Exception e)
            {
                _relayJoined = false;
                Debug.LogException(e);
            }
        }

        private void HandleUpdateLobbyList()
        {
            _lobbyListUpdateTimer -= Time.deltaTime;
            if (_lobbyListUpdateTimer < 0)
            {
                _lobbyListUpdateTimer = _lobbyListUpdateTimerMax;

                ListLobbies();
            }
        }

        #endregion
        
        #region StartGame
        
        public void StartingGame() => StartGame();
    
        public async void StartGame()
        {
            if (!IsLobbyHost()) return;

            try
            {
                Debug.Log($"[StartGame] NetworkManager avant CreateRelay = {NetworkManager.Singleton}");

                string relayCode = await RelayManager.instance.CreateRelay();

                Debug.Log($"[StartGame] Relay créé : {relayCode}");
                Debug.Log($"[StartGame] NetworkManager après CreateRelay = {NetworkManager.Singleton}");

                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("[StartGame] NetworkManager.Singleton est NULL après CreateRelay !");
                    return;
                }

                _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                    _joinedLobby.Id,
                    new UpdateLobbyOptions
                    {
                        IsLocked = true,
                        Data = new Dictionary<string, DataObject>
                        {
                            { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) },
                            { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Member, _selectedMap) }
                        }
                    });

                Debug.Log($"[StartGame] Lobby mis à jour");
                Debug.Log($"[StartGame] NetworkManager = {NetworkManager.Singleton}");
                Debug.Log($"[StartGame] IsHost = {NetworkManager.Singleton.IsHost}");
                Debug.Log($"[StartGame] IsListening = {NetworkManager.Singleton.IsListening}");

                int expected = _joinedLobby.Players.Count;

                float timeout = 20f;

                while (NetworkManager.Singleton != null &&
                       NetworkManager.Singleton.ConnectedClientsIds.Count < expected &&
                       timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    await Task.Yield();
                }

                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("[StartGame] NetworkManager détruit/disparu pendant l'attente !");
                    return;
                }

                Debug.Log(
                    $"[StartGame] Clients connectés : " +
                    $"{NetworkManager.Singleton.ConnectedClientsIds.Count}/{expected}"
                );

                string map = _joinedLobby.Data[KEY_MAP].Value;

                NetworkManager.Singleton.SceneManager.LoadScene(
                    map,
                    LoadSceneMode.Single
                );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        #endregion

        #region Create Lobby

        //Create a lobby
        private async void CreateLobby(string lobbyName)
        {
            try
            {
                CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = GetNewPlayer(),
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                        { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Member, _selectedMap) },
                    }
                };

                if (lobbyName == string.Empty)
                    lobbyName = "Lobby " + Random.Range(0, 99);
                
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYER, createLobbyOptions);

                _hostLobby = lobby;
                _joinedLobby = lobby;

                OnJoinLobby?.Invoke(_joinedLobby, true);

                Debug.Log("Lobby created !! " + lobby.Name + " / " + lobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        #endregion

        #region Join Lobby

        //Method to join a lobby by id (useful when we click on the lobby button of the list)
        private async void JoinLobbyById(string id)
        {
            try
            {
                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                {
                    Player = GetNewPlayer()
                };

                Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(id, options);
                _joinedLobby = lobby;
                
                OnJoinLobby.Invoke(_joinedLobby, false);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        #endregion

        #region Update Lobby List

        private async void ListLobbies()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                return;

            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    },

                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);

                //Invoke the update UI delegate
                OnUpdateJoinedLobby?.Invoke(queryResponse.Results);
            }
            catch (LobbyServiceException e)
            {
                //Ignore
            }
        }

        #endregion

        #region Manage Lobby

        private async void LeaveLobby()
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        private async void KickPlayer()
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, _joinedLobby.Players[1].Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        private async void MigrateLobbyHost()
        {
            try
            {
                _hostLobby = await LobbyService.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
                {
                    HostId = _joinedLobby.Players[1].Id,
                });
                _joinedLobby = _hostLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        private async void DeleteLobby()
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        #endregion

        #region Other Methods

        //Create a new Player for the lobby
        private Player GetNewPlayer()
        {
            return new Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
                }
            };
        }

        bool IsLobbyHost()
        {
            if (_joinedLobby != null)
                return _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

            return false;
        }
        
        public void SetMapButton(string id) => SetMap(id);
  
        public async void SetMap(string newMapName)
        {
            if (!IsLobbyHost()) return;

            try
            {
                ABPrint.Print("try to update map : " + newMapName, ABColor.Red);
                
                await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Member, newMapName) }
                    }
                });

                _selectedMap = newMapName;
                OnUpdatedMap?.Invoke(newMapName);
                
                ABPrint.Print("Map updated : " + newMapName, ABColor.Yellow);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }


        #endregion
    }
}
