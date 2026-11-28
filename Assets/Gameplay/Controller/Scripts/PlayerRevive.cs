using System.Collections.Generic;
using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerRevive : NetworkBusListener
    {
        private static readonly Dictionary<ulong, PlayerRevive> ByOwner = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ByOwner.Clear();

        public static bool TryGet(ulong ownerClientId, out PlayerRevive revive)
            => ByOwner.TryGetValue(ownerClientId, out revive) && revive != null;

        [Header("References")]
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Collider _interactZone;

        [Header("Resurrection")]
        [SerializeField] private float reviveDistance = 2.5f;
        [SerializeField] private float reviveDuration = 4f;
        [SerializeField] private float progressDecayPerSecond = 0.1f;
        [SerializeField] private float heartbeatInterval = 0.1f;
        [SerializeField] private float heartbeatTimeout = 0.35f;

        private readonly NetworkVariable<float> _reviveProgress = new(0f,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private bool _localHolding;
        private float _heartbeatTimer;
        
        private PlayerRevive _reviver;
        private float _lastHeartbeatTime = float.NegativeInfinity;

        public PlayerHealth Health => _health;
        public float ReviveProgress => _reviveProgress.Value;
        public bool IsDowned => _health != null && _health.IsDowned;
        
        private Vector3 PlayerPosition => NetworkObject.transform.position;

        public override void OnNetworkSpawn()
        {
            if (_health == null) _health = GetComponentInParent<PlayerHealth>();
            if (_health == null) _health = GetComponentInChildren<PlayerHealth>();
            if (_health == null)
            {
                Debug.LogError("PlayerRevive: no PlayerHealth found.", this);
                enabled = false;
                return;
            }

            ByOwner[OwnerClientId] = this;

            _reviveProgress.OnValueChanged += OnProgressChanged;
            _health.OnChangedDown += OnDownedChanged;
            SetZoneActive(_health.IsDowned); 
        }

        public override void OnNetworkDespawn()
        {
            if (ByOwner.TryGetValue(OwnerClientId, out var current) && current == this)
                ByOwner.Remove(OwnerClientId);

            _reviveProgress.OnValueChanged -= OnProgressChanged;
            if (_health != null) _health.OnChangedDown -= OnDownedChanged;
            _localHolding = false;

            base.OnNetworkDespawn();
        }

        private void OnDownedChanged(bool downed) => SetZoneActive(downed);

        private void SetZoneActive(bool active)
        {
            if (_interactZone != null) _interactZone.enabled = active;
        }

        private void Update()
        {
            if (!IsSpawned || _health == null) return;
            if (_localHolding) UpdateLocalHold();
            if (IsServer) UpdateServerRevive();
        }

        private void OnProgressChanged(float previous, float current)
        {
            InvokeEvent(new ReviveChanged_EVENT
            {
                PlayerId = OwnerClientId,
                Reviving = current > previous,
                Percent = current
            });
        }

        #region Reviver side (called by PlayerReviveInteractable on the local client)

        public void LocalHoldStart()
        {
            _localHolding = true;
            _heartbeatTimer = 0f; 
        }

        public void LocalHoldEnd() => _localHolding = false;

        private void UpdateLocalHold()
        {
            if (!IsDowned) { _localHolding = false; return; }

            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer > 0f) return;

            _heartbeatTimer = heartbeatInterval;
            ReviveHeartbeatRpc();
        }

        
        [Rpc(SendTo.Server)]
        private void ReviveHeartbeatRpc(RpcParams rpcParams = default)
        {
            if (!_health.IsDowned) return;

            ulong sender = rpcParams.Receive.SenderClientId;
            if (sender == OwnerClientId) return;              
            if (!TryGet(sender, out var reviver)) return;    

            _reviver = reviver;
            _lastHeartbeatTime = Time.time;
        }

        #endregion

        #region Revivable (server, on the downed player's object)

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
            if (_reviver == null || !_reviver.IsSpawned) return false;
            if (_reviver.Health == null || _reviver.Health.IsDowned) return false;

            float maxDist = reviveDistance * 1.5f;
            return (_reviver.PlayerPosition - PlayerPosition).sqrMagnitude <= maxDist * maxDist;
        }

        #endregion
    }
}