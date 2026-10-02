using System.Collections.Generic;
using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerResurrection : NetworkBusListener
    {
        public static readonly List<PlayerResurrection> All = new();

        [Header("Resurrection")]
        [SerializeField] private float reviveDistance = 2.5f;
        [SerializeField] private float reviveDuration = 4f;
        [SerializeField] private float progressDecayPerSecond = 0.1f;
        [SerializeField] private float heartbeatInterval = 0.1f;
        [SerializeField] private float heartbeatTimeout = 0.35f;

        [HideInInspector] public NetworkVariable<bool> IsDowned = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> ReviveProgress = new(0f,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        
        private PlayerInput _playerInput;
        private bool _interactHeld;
        private PlayerResurrection _currentTarget;
        private float _heartbeatTimer;

        
        private ulong _reviverId;
        private float _lastHeartbeatTime = float.NegativeInfinity;

        public override void OnNetworkSpawn()
        {
            All.Add(this);

            if (!IsOwner) return;

            _playerInput = TryGetComponent(out PlayerInput pi) ? pi : gameObject.AddComponent<PlayerInput>();
            _playerInput.OnInteractInput += OnInteractPressed;
            _playerInput.OnInteractInputCanceled += OnInteractReleased;

            ListenToEvent<PlayerDeathEvent>(OnPlayerDeath);
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);

            if (_playerInput != null)
            {
                _playerInput.OnInteractInput -= OnInteractPressed;
                _playerInput.OnInteractInputCanceled -= OnInteractReleased;
            }

            base.OnNetworkDespawn();
        }

        private void OnInteractPressed() => _interactHeld = true;
        private void OnInteractReleased() => _interactHeld = false;

        private void OnPlayerDeath(PlayerDeathEvent e)
        { 
            if(!IsOwner) return;
            if (e.playerID == OwnerClientId)
                IsDowned.Value = true;
        }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) UpdateReviver();
            if (IsServer) UpdateServerRevive();
        }

        #region Reviver (owner client)

        private void UpdateReviver()
        {
            PlayerResurrection best = null;
            if (_interactHeld && !IsDowned.Value)
                best = FindClosestDownedAlly();
            
            if (best != _currentTarget)
            {
                _currentTarget = best;
                _heartbeatTimer = 0f;
            }

            if (_currentTarget == null) return;

            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f)
            {
                _heartbeatTimer = heartbeatInterval;
                ReviveHeartbeatRpc(_currentTarget.NetworkObject);
            }
        }

        private PlayerResurrection FindClosestDownedAlly()
        {
            PlayerResurrection best = null;
            float bestSqr = reviveDistance * reviveDistance;

            foreach (var other in All)
            {
                if (other == this || !other.IsSpawned || !other.IsDowned.Value) continue;

                float sqr = (other.transform.position - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = other;
                }
            }
            return best;
        }

        [Rpc(SendTo.Server)]
        private void ReviveHeartbeatRpc(NetworkObjectReference targetRef, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

            if (targetRef.TryGet(out NetworkObject targetObj) &&
                targetObj.TryGetComponent(out PlayerResurrection target))
            {
                target.ServerReceiveHeartbeat(OwnerClientId);
            }
        }

        #endregion

        #region Server (on the downed player's object)

        private void ServerReceiveHeartbeat(ulong reviverId)
        {
            if (!IsDowned.Value) return;
            _reviverId = reviverId;
            _lastHeartbeatTime = Time.time;
        }

        private void UpdateServerRevive()
        {
            if (!IsDowned.Value)
            {
                _lastHeartbeatTime = float.NegativeInfinity;
                if (ReviveProgress.Value != 0f) ReviveProgress.Value = 0f;
                return;
            }

            if (IsReviverStillValid())
            {
                ReviveProgress.Value = Mathf.Min(1f, ReviveProgress.Value + Time.deltaTime / reviveDuration);

                if (ReviveProgress.Value >= 1f)
                {
                    ReviveProgress.Value = 0f;
                    _lastHeartbeatTime = float.NegativeInfinity;
                    ReviveOwnerRpc();
                }
            }
            else if (ReviveProgress.Value > 0f)
            {
                ReviveProgress.Value = Mathf.Max(0f, ReviveProgress.Value - progressDecayPerSecond * Time.deltaTime);
            }
        }

        private bool IsReviverStillValid()
        {
            if (Time.time - _lastHeartbeatTime > heartbeatTimeout) return false;
            if (!NetworkManager.ConnectedClients.TryGetValue(_reviverId, out var client)) return false;
            if (client.PlayerObject == null) return false;
            if (!client.PlayerObject.TryGetComponent(out PlayerResurrection reviver)) return false;
            if (reviver.IsDowned.Value) return false;

            float maxDist = reviveDistance * 1.5f;
            return (reviver.transform.position - transform.position).sqrMagnitude <= maxDist * maxDist;
        }

        #endregion

        [Rpc(SendTo.Owner)]
        public void ReviveOwnerRpc()
        {
            IsDowned.Value = false;

            if (TryGetComponent(out PlayerController controller))
                controller.Revive();
            ReviveServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void ReviveServerRpc()
        {
            InvokeEvent(new PlayerRevivedEvent { playerID = OwnerClientId });
        }
    }
}