using Bus;
using Unity.Netcode;
using Unity.VisualScripting;
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
        
        private readonly NetworkVariable<bool> _canOpen = new(false);
        private readonly NetworkVariable<bool> _isOpen = new(false);

        public override void OnNetworkSpawn()
        {
            _isOpen.OnValueChanged += OnOpenChanged;

            if (IsServer)
            {
                _canOpen.Value = _canOpenWithoutKey;
            }
        }

        public override void OnNetworkDespawn()
        {
            _isOpen.OnValueChanged -= OnOpenChanged;
        }

        private void OnOpenChanged(bool previous, bool current)
        {
            if (current)
            {
                _eventOpen?.Invoke();
            }else
            {
                _eventClose?.Invoke();
            }
        }
        
        public bool CanOpenWithoutKey { get => _canOpen.Value; }

        public bool TryOpen(Vector2Int idRange)
        {
            if (!IsServer) return false;

            if (!_canOpen.Value)
            {
                _isOpen.Value = true;
                _canOpen.Value = true;
                return true;
            }

            if (idRange is { x: -1, y: -1 })
                return false;

            if ((_doorID < idRange.x || _doorID > idRange.y))
                return false;

            _isOpen.Value = !_isOpen.Value;
            return true;
        }
    }
}