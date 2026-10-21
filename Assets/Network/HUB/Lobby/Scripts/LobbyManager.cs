using System.Threading.Tasks;
using Bus;
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

    public class LobbyManager : MonoBusListener
    {
        public static int MAX_PLAYER_COUNT => MAX_PLAYER;
        const int MAX_PLAYER = 4;
        
        //Lobby var
        private Lobby _hostLobby;
        private Lobby _joinedLobby;
        
        //KeyDataName
        private string KEY_RELAY = "Relay";
        private string KEY_MAP = "Map";
        private string KEY_PLAYER_NAME = "PlayerName";
        private string KEY_PLAYER_COLOR = "ColorId";
        private string KEY_PLAYER_READY = "IsReady";

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
        public Action<List<Player>> OnUpdateLobbyInfo;
        public Action<string> OnUpdatedMap;
        public Action OnTryToJoinGame;
        public Action OnLeaveLobby;

        private async void Start()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SwitchProfile("player_" + Random.Range(0, 100000));
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                _playerName = "...";
                
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

        private void ResetLobbyState()
        {
            _hostLobby = null;
            _joinedLobby = null;
            _relayJoined = false;
            _heartbeatTimer = 0f;
            _lobbyUpdateTimer = 0f;

            OnLeaveLobby?.Invoke();
        }
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
               Debug.Log(e);
            }
        }

        //Catch the new Lobby with the modification that was applies to it (synchronize update)
        private async void HandleLobbyPollForUpdates()
        {
            if (_joinedLobby == null) return;

            _lobbyUpdateTimer -= Time.deltaTime;
            if (_lobbyUpdateTimer >= 0) return;
            _lobbyUpdateTimer = _lobbyUpdateTimerMax;

            string lobbyId = _joinedLobby.Id;
            
            try
            {
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                
                if (_joinedLobby == null || _joinedLobby.Id != lobbyId) return;
                
                _joinedLobby = lobby;

                OnUpdateLobbyInfo?.Invoke(_joinedLobby.Players);
                
                if (IsLobbyHost() || _relayJoined) return;

                if (_joinedLobby.Data.TryGetValue(KEY_RELAY, out var relayData) && relayData.Value != "0")
                {
                    OnTryToJoinGame?.Invoke();
                    
                    _relayJoined = true;
                    _relayJoined = await RelayManager.instance.JoinRelay(relayData.Value);
                }
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound || e.Reason == LobbyExceptionReason.Forbidden)
            {
                if (_joinedLobby != null && _joinedLobby.Id == lobbyId)
                    ResetLobbyState();
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

            if (!CheckAllPlayerReady()) return;

            InvokeEvent(new OnStartCreateGame_EVENT());
            
            try
            {
                Player me = _joinedLobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId);
                
                if (me.Data.ContainsKey(KEY_PLAYER_NAME) && _playerName.Equals("..."))
                {
                    await LobbyService.Instance.UpdatePlayerAsync(
                        _joinedLobby.Id,
                        AuthenticationService.Instance.PlayerId,
                        new UpdatePlayerOptions
                        {
                            Data = new Dictionary<string, PlayerDataObject>
                            {
                                { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "Player : " + Random.Range(0, 99)) }
                            }
                        });
                }
                
                string relayCode = await RelayManager.instance.CreateRelay();
                if (NetworkManager.Singleton == null)
                {
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
                    return;
                }

                string map = _joinedLobby.Data[KEY_MAP].Value;

                if (NetworkManager.Singleton == null || _joinedLobby == null) return;

                string idToDelete = _joinedLobby.Id;
                ResetLobbyState(); 

                try { await LobbyService.Instance.DeleteLobbyAsync(idToDelete); }
                catch (LobbyServiceException e) { Debug.Log(e); }
                
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
                Debug.Log(e);
            }
        }

        #endregion

        #region Manage Lobby

        public void LeaveCurrentLobby() => LeaveLobby();
        private async void LeaveLobby()
        {
            if (_joinedLobby == null) return;

            string lobbyId = _joinedLobby.Id;
            bool wasHost = IsLobbyHost(); 
            
            ResetLobbyState();

            try
            {
                if (wasHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
                }
                    
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
                    { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) },
                    { KEY_PLAYER_COLOR,     new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                    { KEY_PLAYER_READY,     new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") }
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
  
        private async void SetMap(string newMapName)
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
        
        public void SetColorButton(int newSkinId) => SetColor(newSkinId);
        
        private async void SetColor(int newSkinId)
        {
            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(
                    _joinedLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { KEY_PLAYER_COLOR, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newSkinId.ToString()) }
                        }
                    });

                Debug.Log("Skin updated: " + newSkinId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        
        public void SetNameButton(string newName) => SetName(newName);
        
        private async void SetName(string newName)
        {
            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(
                    _joinedLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newName) }
                        }
                    });
                
                _playerName = newName;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        
        public void SetReadyButton() => SetReady();
        
        public async void SetReady()
        {
            try
            {
                Player me = _joinedLobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId);

                if (me == null)
                {
                    Debug.LogError("Local player not found in lobby");
                    return;
                }

                string current  = me.Data.ContainsKey(KEY_PLAYER_READY) ? me.Data[KEY_PLAYER_READY].Value : "0";
                string newState = current == "0" ? "1" : "0";

                await LobbyService.Instance.UpdatePlayerAsync(
                    _joinedLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { KEY_PLAYER_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newState) }
                        }
                    });

                Debug.Log(KEY_PLAYER_READY + newState);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        
        public bool CheckAllPlayerReady()
        {
            if (_joinedLobby == null) return false;
            if (_joinedLobby.Players == null || _joinedLobby.Players.Count == 0) return false;

            foreach (Player player in _joinedLobby.Players)
            {
                if (player.Data == null ||
                    !player.Data.ContainsKey(KEY_PLAYER_READY) ||
                    player.Data[KEY_PLAYER_READY].Value != "1")
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
