using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace Gameplay.LD.Proto1
{
    public class HoldButtonDoor : OnlineInteractable, IHoldInteractable
    {
        public NetworkVariable<bool> Activated = new NetworkVariable<bool>(false);
        public event Action StateChanged;

        private readonly HashSet<ulong> _holders = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            base.OnNetworkDespawn();
        }
        
        public void HoldStart(ulong clientId)
        {
            if (IsServer) ServerHoldStart(clientId);
            else HoldStartServerRpc();
        }
        
        public void HoldEnd(ulong clientId)
        {
            if (IsServer) ServerHoldEnd(clientId);
            else HoldEndServerRpc();
        }
        
        [Rpc(SendTo.Server)]
        void HoldStartServerRpc(RpcParams rpcParams = default) => ServerHoldStart(rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.Server)]
        void HoldEndServerRpc(RpcParams rpcParams = default) => ServerHoldEnd(rpcParams.Receive.SenderClientId);
        
        void ServerHoldStart(ulong clientId)
        {
            _holders.Add(clientId);
            Refresh();
        }

        void ServerHoldEnd(ulong clientId)
        {
            _holders.Remove(clientId);
            Refresh();
        }
        
        void OnClientDisconnected(ulong clientId)
        {
            if (_holders.Remove(clientId)) Refresh();
        }

        void Refresh()
        {
            bool shouldBeActive = _holders.Count > 0;
            if (shouldBeActive == Activated.Value) return;

            Activated.Value = shouldBeActive;
            if (shouldBeActive) OpenEventsRpc();
            else CloseEventsRpc();
            StateChanged?.Invoke();
        }
        
        protected override void ServerInteract(ulong senderClientId) { }
        protected override void OnInteract() { }
    }
}