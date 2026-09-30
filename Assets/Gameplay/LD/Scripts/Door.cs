using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public class Door : NetworkBusListener
    {
        [SerializeField] private int _doorID;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _openStateName = "Open";

        private readonly NetworkVariable<bool> _isOpen = new(false);

        public override void OnNetworkSpawn()
        {
            _isOpen.OnValueChanged += OnOpenChanged;

            if (_isOpen.Value)
                _animator.Play(_openStateName, 0, 1f);
        }

        public override void OnNetworkDespawn()
        {
            _isOpen.OnValueChanged -= OnOpenChanged;
        }

        private void OnOpenChanged(bool previous, bool current)
        {
            if (current)
                _animator.SetTrigger("Open");
        }

        public bool TryOpen(int id)
        {
            if (!IsServer) return false;
            if (_isOpen.Value) return false;
            if (id != _doorID) return false;

            _isOpen.Value = true;
            return true;
        }
    }
}