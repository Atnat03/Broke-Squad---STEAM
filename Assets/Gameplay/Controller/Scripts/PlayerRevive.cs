using System.Collections.Generic;
using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerRevive : NetworkBusListener
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private static readonly List<PlayerRevive> All = new();

        [Header("Resurrection")]
        [SerializeField] private float reviveDistance = 2.5f;
        [SerializeField] private float reviveDuration = 4f;
        [SerializeField] private float progressDecayPerSecond = 0.1f;
        [SerializeField] private float heartbeatInterval = 0.1f;
        [SerializeField] private float heartbeatTimeout = 0.35f;

        private readonly NetworkVariable<float> _reviveProgress = new(0f,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerHealth _health;
        private PlayerInput _playerInput;
        
        private bool _interactHeld;
        private PlayerRevive _currentTarget;
        private float _heartbeatTimer;
        
        private ulong _reviverId;
        private float _lastHeartbeatTime = float.NegativeInfinity;
        
        public PlayerHealth Health => _health;
        public float ReviveProgress => _reviveProgress.Value;

        public override void OnNetworkSpawn()
        {
            _health = GetComponent<PlayerHealth>();
            All.Add(this);

            if (!IsOwner) return;

            _playerInput = TryGetComponent(out PlayerInput pi) ? pi : gameObject.AddComponent<PlayerInput>();
            _playerInput.OnInteractInput += OnInteractPressed;
            _playerInput.OnInteractInputCanceled += OnInteractReleased;
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
        

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) UpdateReviver();
            if (IsServer) UpdateServerRevive();
        }

        #region Reviver (owner client)

        private void UpdateReviver()
        {
            PlayerRevive best = null;
            if (_interactHeld && !_health.IsDowned)
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

        private PlayerRevive FindClosestDownedAlly()
        {
            PlayerRevive best = null;
            float bestSqr = reviveDistance * reviveDistance;

            foreach (var other in All)
            {
                if (other == this || !other.IsSpawned || other.Health == null || !other.Health.IsDowned) continue;

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
                targetObj.TryGetComponent(out PlayerRevive target))
            {
                target.ServerReceiveHeartbeat(OwnerClientId);
            }
        }

        #endregion

        #region Revivable (server, on the downed player's object)

        private void ServerReceiveHeartbeat(ulong reviverId)
        {
            if (!_health.IsDowned) return;
            _reviverId = reviverId;
            _lastHeartbeatTime = Time.time;
        }

        private void UpdateServerRevive()
        {
            if (!_health.IsDowned)
            {
                _lastHeartbeatTime = float.NegativeInfinity;
                if (_reviveProgress.Value != 0f) _reviveProgress.Value = 0f;
                return;
            }

            if (IsReviverStillValid())
            {
                _reviveProgress.Value = Mathf.Min(1f, _reviveProgress.Value + Time.deltaTime / reviveDuration);

                if (_reviveProgress.Value >= 1f)
                {
                    _reviveProgress.Value = 0f;
                    _lastHeartbeatTime = float.NegativeInfinity;
                    _health.ServerRevive();          
                }
            }
            else if (_reviveProgress.Value > 0f)
            {
                _reviveProgress.Value = Mathf.Max(0f, _reviveProgress.Value - progressDecayPerSecond * Time.deltaTime);
            }
        }

        private bool IsReviverStillValid()
        {
            if (Time.time - _lastHeartbeatTime > heartbeatTimeout) return false;
            if (!NetworkManager.ConnectedClients.TryGetValue(_reviverId, out var client)) return false;
            if (client.PlayerObject == null) return false;
            if (!client.PlayerObject.TryGetComponent(out PlayerRevive reviver)) return false;
            if (reviver.Health == null || reviver._health.IsDowned) return false;

            float maxDist = reviveDistance * 1.5f;
            return (reviver.transform.position - transform.position).sqrMagnitude <= maxDist * maxDist;
        }

        #endregion
        
       
    }
}