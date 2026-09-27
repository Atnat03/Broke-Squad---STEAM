using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Network.Connections
{
    public class RelayManager : MonoBehaviour
    {
        public static string LastJoinCode;
        public static RelayManager instance;

        private void Awake()
        {
            instance = this;
        }

        async Task InitializeServices()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                
            } 
            if (!AuthenticationService.Instance.IsSignedIn) 
            { 
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); 
                Debug.Log("Signed In: " + AuthenticationService.Instance.PlayerId); 
            }
        }
        
        #region Create a relay

        public async Task<string> CreateRelay()
        {
            try
            {
                await InitializeServices(); 

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                LastJoinCode = joinCode;
                Debug.Log("Join Code généré: " + joinCode);

                var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
                utp.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

                NetworkManager.Singleton.StartHost();

                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError("Erreur dans CreateRelay: " + e.Message);
                return null;
            }
        }

        #endregion
        
        #region Join a relay
        
        public async Task JoinRelay(string joinCode)
        {
            try
            {
                await InitializeServices();
                if (string.IsNullOrEmpty(joinCode)) return;

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
                var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
                utp.SetRelayServerData(relayServerData);
                
                NetworkManager.Singleton.StartClient();
                StartCoroutine(CheckConnectionAfterDelay());
            }
            catch (Exception e)
            {
                Debug.LogError("Erreur dans JoinRelay: " + e.Message);
            }
        }
        
        #endregion
        
        #region Disconneted
        
        private void OnApplicationQuit()
        {
            DisconnectRelay();
        }
        
        void DisconnectRelay()
        {
            if (NetworkManager.Singleton == null) return;

            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Host Relay arrêté et déconnecté.");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Client Relay arrêté et déconnecté.");
            }
        }
        
        private IEnumerator CheckConnectionAfterDelay()
        {
            yield return new WaitForSeconds(10f);
            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.LogError("Connexion échouée !");
            }
        }
        
        #endregion
    }
}