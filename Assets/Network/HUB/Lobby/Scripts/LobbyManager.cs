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

        [SerializeField] private string _sceneNameToPlayTogether;
        
        //Lobby var
        private Lobby _hostLobby;
        private Lobby _joinedLobby;

        //Current time before update Lobbies
        private float _heartbeatTimer;
        private float _lobbyUpdateTimer;
        private float _lobbyListUpdateTimer;

        //Timer to update Lobbies 
        [SerializeField] private float _lobbyUpdateTimerMax = 2f;
        [SerializeField] private float _heartbeatTimerMax = 3f;
        [SerializeField] private float _lobbyListUpdateTimerMax = 1.5f;

        //Other variables
        private string _playerName;

        //Actions
        public Action<Lobby, bool> OnJoinLobby;
        public Action<List<Lobby>> OnUpdateJoinedLobby;
        public Action<Lobby> OnUpdateLobbyInfo;

        private async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();

                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                _playerName = "Player " + Random.Range(0, 99);
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
            try
            {
                if (_joinedLobby != null)
                {
                    _lobbyUpdateTimer -= Time.deltaTime;
                    if (_lobbyUpdateTimer < 0)
                    {
                        _lobbyUpdateTimer = _lobbyUpdateTimerMax;

                        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
                        _joinedLobby = lobby;
                        
                        OnUpdateLobbyInfo.Invoke(_joinedLobby);
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
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

        public async void StartGame()
        {
            if (!IsLobbyHost()) return;

            try
            {
                ABPrint.Print("Start Game", ABColor.Yellow);

                int    mySkin  = 0;
                string myName  = _playerName;
                Player me = _joinedLobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId);
                
                string relayCode = await RelayManager.instance.CreateRelay();

                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "Relay", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                _joinedLobby = lobby;
                
                NetworkManager.Singleton.SceneManager.LoadScene(_sceneNameToPlayTogether, LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

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
                        { "Relay", new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    }
                };

                if (lobbyName == string.Empty)
                    lobbyName = "Lobby " + Random.Range(0, 99);
                
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYER, createLobbyOptions);

                _hostLobby = lobby;
                _joinedLobby = lobby;

                OnJoinLobby.Invoke(_joinedLobby, true);

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
                OnUpdateJoinedLobby.Invoke(queryResponse.Results);
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

        #endregion
    }
}
