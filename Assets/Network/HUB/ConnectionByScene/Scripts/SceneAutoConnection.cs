using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Transactions;
using Network.Connections;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network.HUB
{
    public class SceneAutoConnection : MonoBehaviour
    {
        private int _selectedSceneId = -1;
        
        private string _gameCode = string.Empty;
        
        public Action<string> OnSelectedSceneChanged;

        void Awake()
        {
            // Set the data file to empty to avoid some potential error
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "relay_code.txt"), string.Empty);
        }

        void Start()
        {
            // If i'm a Vritual player, try to connect myself as a client
            if (!CurrentPlayer.Tags.Contains("Server"))
            {
                StartCoroutine(StartClient());
            }
        }
        
        private IEnumerator StartClient()
        {
            yield return StartCoroutine(GetCodeFromFile());

            JoinRelay();
        }

        // Loop to catch the connection code
        IEnumerator GetCodeFromFile()
        {
            while (true)
            {
                try
                {
                    _gameCode = File.ReadAllText(Path.Combine(Application.persistentDataPath, "relay_code.txt"));

                    if (!string.IsNullOrEmpty(_gameCode))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Waiting for code..." + e.Message);
                }
            
                yield return new WaitForSeconds(1f);
            }
        }

        // Join the relay
        private async void JoinRelay()
        {
            try
            {
                await RelayManager.instance.JoinRelay(_gameCode);
            }
            catch (Exception e)
            {
                Debug.Log("JoinRelay Error : " + e);
            }
        }
        
        public void SelectScene(int id)
        {
            _selectedSceneId = id;
        
            string sceneName = SceneUtility.GetScenePathByBuildIndex(id);

            OnSelectedSceneChanged?.Invoke(Path.GetFileNameWithoutExtension(sceneName));
        }
        
        public void TryConnect()
        {
            if (_selectedSceneId == -1)
                return;
        
            bool isServer = CurrentPlayer.Tags.Contains("Server_2") || 
                            CurrentPlayer.Tags.Contains("Server_3") ||
                            CurrentPlayer.Tags.Contains("Server_4") ||
                            CurrentPlayer.Tags.Contains("Server");
        
            if (isServer)
            {
                StartHost();
            }
        
            StartCoroutine(WaitForAllPlayerConnected());
        }
        
        IEnumerator WaitForAllPlayerConnected()
        {
            int numberPlayer = GetNumberPlayer();
        
            while (true)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count == numberPlayer)
                {
                    break;
                }
                
                yield return new WaitForSeconds(0.5f);
            }

            if(NetworkManager.Singleton.IsServer)
            {
                string sceneName = SceneUtility.GetScenePathByBuildIndex(_selectedSceneId);
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

        int GetNumberPlayer()
        {
            if (CurrentPlayer.Tags.Contains("Server_2"))
                return 2;
            if (CurrentPlayer.Tags.Contains("Server_3"))
                return 3;
            if (CurrentPlayer.Tags.Contains("Server_4"))
                return 4;

            return 1;
        }
    
        async void StartHost()
        {
            try
            {
                await RelayManager.instance.CreateRelay();

                _gameCode = RelayManager.LastJoinCode;
            
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "relay_code.txt"), _gameCode);
            }
            catch (Exception e)
            {
                Debug.LogError("StartHost Error: " + e);
            }
        }
    }
}