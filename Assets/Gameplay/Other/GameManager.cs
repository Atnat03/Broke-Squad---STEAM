using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Items;
using Gameplay.PlayerData;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Other
{
    public struct OnGameEnded_EVENT
    {
        public bool Win;
    }
    
    public class GameManager : NetworkBusListener
    {
        private bool _hasGoal = false;
        
        private readonly NetworkVariable<bool> _win = new();
        private readonly NetworkVariable<bool> _loose = new();
        
        public override void OnNetworkSpawn()
        {
            _win.OnValueChanged += OnWin;
            _loose.OnValueChanged += OnLoose;
            
            ListenToEvent<OnTryToWin_EVENT>(TryToWin);

            if (IsServer)
            {
                ListenToEvent<OnGrabGoal_EVENT>(SetHasGoal);
                ListenToEvent<OnDropGoal_EVENT>(SetDropGoal);
                ListenToEvent<DownedChanged_EVENT>(_ => CheckAllPlayersDown());
                
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            _win.OnValueChanged -= OnWin;
            _loose.OnValueChanged -= OnLoose;
            
            if (IsServer)
            {
                if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            base.OnNetworkDespawn();
        }
        
        
        private void OnClientDisconnected(ulong clientId) => CheckAllPlayersDown();

        private void CheckAllPlayersDown()
        {
            if (!IsServer || _loose.Value || _win.Value) return;
            
            int total = 0;
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null) continue;  
                if (!playerObject.TryGetComponent(out PlayerHealth health)) continue;

                total++;
                if (!health.IsDowned) return;         
            }

            if (total == 0) return;  

            ABPrint.Print("All players are down", ABColor.Red);
            _loose.Value = true;
        }

        
        private void SetHasGoal(OnGrabGoal_EVENT data)
        {
            _hasGoal = true;   
        }
        private void SetDropGoal(OnDropGoal_EVENT data) => _hasGoal = false;
        
        private void OnWin(bool previous, bool current)
        {
            if (current) InvokeEvent(new OnGameEnded_EVENT { Win = true });
        }

        private void OnLoose(bool previous, bool current)
        {
            if (current) InvokeEvent(new OnGameEnded_EVENT { Win = false });
        }
        
        void TryToWin(OnTryToWin_EVENT data)
        {
            TryWin();
        }
        
        void TryWin()
        {
            if (!IsServer)
            {
                TryWinRpc();
                return;
            }

            if (_win.Value || _loose.Value) return;
            
            if (_hasGoal)
            {
                ABPrint.Print("WIN !!", ABColor.Green);
            }
            else
            {
                ABPrint.Print("U cant win, u dont have the goal !!", ABColor.Red);
            }
        }

        [Rpc(SendTo.Server)]
        private void TryWinRpc() => TryWin();
    }
}