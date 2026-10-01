using Bus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD.Scripts
{
    public class Door : NetworkBusListener
    {
        [SerializeField] private int _doorID;
        [SerializeField] private UnityEvent _eventOpen;
        [SerializeField] private UnityEvent _eventClose;
        [SerializeField] private bool _canOpenWithoutKey = false;
        [SerializeField] private float _toggleCooldown = 0.3f;

        private readonly NetworkVariable<bool> _isUnlocked = new(false);
        private readonly NetworkVariable<bool> _isOpen = new(false);
        
        public bool CanOpenWithoutKey => _isUnlocked.Value;

        private float _lastToggleTime = -10f;

        public override void OnNetworkSpawn()
        {
            _isOpen.OnValueChanged += OnOpenChanged;

            if (IsServer)
            {
                _isUnlocked.Value = _canOpenWithoutKey;
            }
        }

        public override void OnNetworkDespawn()
        {
            _isOpen.OnValueChanged -= OnOpenChanged;
        }

        private void OnOpenChanged(bool previous, bool current)
        {
            if (current) _eventOpen?.Invoke();
            else _eventClose?.Invoke();
        }

        public bool TryOpen(Vector2Int idRange)
        {
            if (!IsServer) return false;

            if (Time.time - _lastToggleTime < _toggleCooldown)
                return false;

            if (!_isUnlocked.Value)
            {
                if (!IsKeyValid(idRange))
                    return false;

                _isUnlocked.Value = true;
            }

            _lastToggleTime = Time.time;
            _isOpen.Value = !_isOpen.Value;
            return true;
        }

        private bool IsKeyValid(Vector2Int idRange)
        {
            if (idRange.x == -1 && idRange.y == -1)
                return false;

            return _doorID >= idRange.x && _doorID <= idRange.y;
        }
    }
}