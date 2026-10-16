using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Proto1.Scripts
{
    public class KeyCondition : NetworkBehaviour, IInteractable
    {
        private readonly NetworkVariable<bool> _isOpened = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            _isOpened.OnValueChanged += OnOpenChange;
        }

        private void OnOpenChange(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                ABPrint.Print("Open", ABColor.Green);
            }
            else
            {
                ABPrint.Print("Close", ABColor.Red);
            }
        }

        [SerializeField] private Outline _outline;
        
        public Outline outline => _outline;
        public void SetOutline(bool state)
        {
            outline.enabled = state;
        }

        public void Interact(ulong clientId)
        {
            Open();
        }

        private void Open()
        {
            if (!IsServer)
            {
                OpenRpc();
                return;
            }
            
            if (_alreadyOpened)
            {
                _isOpened.Value = !_isOpened.Value;
            }
            
            if (TryOpen(Range))
            {
                ABPrint.Print("IsOpen true");

                _isOpened.Value = true;
                _alreadyOpened = true;
            }
        }

        [Rpc(SendTo.Server)]
        private void OpenRpc() => Open();
        
        [Header("Key")] 
        [SerializeField] private int _keyId = 0;
        [SerializeField] private bool _canBeOpenWithoutKey = false;
        [HideInInspector] public Vector2Int Range;
        private bool _alreadyOpened = false;
        
        public bool TryOpen(Vector2Int range)
        {
            if (_alreadyOpened) return true;
            
            if(range == new Vector2Int(-1, -1)) return true;
            
            if (range.x <= _keyId && _keyId <= range.y)
            {
                return true;
            }

            return false;
        }
    }
}