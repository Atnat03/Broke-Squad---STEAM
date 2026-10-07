using Bus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public interface INeedKey
{
    public bool CheckKey(Vector2Int idRange);
}

namespace Gameplay.LD.Scripts
{
    public class Door : NetworkBusListener, INeedKey
    {
        [SerializeField] private int _doorID;
        [SerializeField] private UnityEvent _eventOpen;
        [SerializeField] private UnityEvent _eventClose;
        [SerializeField] private bool _canOpenWithoutKey = false;
        [SerializeField] private float _toggleCooldown = 0.3f;

        [Header("SFX")]
        [SerializeField, SoundName] private string _openSound;
        [SerializeField, SoundName] private string _closeSound;
        [SerializeField, SoundName] private string _tryToOpenClosedDoorSound;
        
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
            string currentAction;
            
            if (current)
            {
                _eventOpen?.Invoke();
                currentAction = _openSound;
            }
            else
            {
                _eventClose?.Invoke();
                currentAction = _closeSound;
            }
            
            InvokeEvent(new PlaySoundEvent
            {
                soundName = currentAction,
                position = transform.position,
                volume = 0.5f
            });
        }

        public bool TryOpen(Vector2Int idRange)
        {
            if (!IsServer)
            { 
                TryOpenRpc(idRange);
                return false;
            }

            if (Time.time - _lastToggleTime < _toggleCooldown)
                return false;

            if (!_isUnlocked.Value)
            {
                if (!IsKeyValid(idRange))
                {
                    ReplicateTryToOpenClosedDoorRpc();
                    return false;
                }

                _isUnlocked.Value = true;
            }

            _lastToggleTime = Time.time;
            _isOpen.Value = !_isOpen.Value;
            return true;
        }

        [Rpc(SendTo.Server)]
        private void TryOpenRpc(Vector2Int idRange)
        {
            TryOpen(idRange);
        }
        
        private bool IsKeyValid(Vector2Int idRange)
        {
            if (idRange.x == -1 && idRange.y == -1)
                return false;

            return _doorID >= idRange.x && _doorID <= idRange.y;
        }

        [Rpc(SendTo.Everyone)]
        private void ReplicateTryToOpenClosedDoorRpc()
        {
            InvokeEvent(new PlaySoundEvent
            {
                soundName = _tryToOpenClosedDoorSound,
                position = transform.position,
                volume = 0.5f
            });
        }

        public bool CheckKey(Vector2Int idRange)
        {
            return TryOpen(idRange);
        }
    }
}