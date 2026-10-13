using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class LockDownManager : NetworkBusListener
    {
        [Header("View")]
        [SerializeField] private GameObject _lockDownPrefab;
        
        public bool IsLockDown => _isLockDown.Value;
        
        private readonly NetworkVariable<bool> _isLockDown = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            _isLockDown.OnValueChanged += OnLockDownChange;
            
            if(IsServer)
                _isLockDown.Value = false;
            
            ListenToEvent<OnLockDownStart_EVENT>(TryStartLockDown);
            ListenToEvent<OnLockDownEnd_EVENT>(TryEndLockDown);
        }

                
        #region Start Lock Down

        private void TryStartLockDown(OnLockDownStart_EVENT ev)
        {
            if (_isLockDown.Value)
                return;
            
            StartLockDown();
        }

        void StartLockDown()
        {
            if (!IsServer)
            {
                StartLockDownServerRpc();
                return;
            }

            _isLockDown.Value = true;
        }

        [Rpc(SendTo.Server)]
        private void StartLockDownServerRpc() => StartLockDown();

        #endregion
        
        #region End Lock Down
        
        private void TryEndLockDown(OnLockDownEnd_EVENT obj)
        {
            if (!_isLockDown.Value)
                return;
            
            EndLockDown();
        }
        
        [ContextMenu("End Lock Down")]
        public void EndLockDown()
        {
            if (!IsServer)
            {
                EndLockDownServerRpc();
                return;
            }

            _isLockDown.Value = false;
        }

        [Rpc(SendTo.Server)]
        private void EndLockDownServerRpc() => EndLockDown();

        #endregion

        
        private void OnLockDownChange(bool previousValue, bool newValue)
        {
            _lockDownPrefab.SetActive(newValue);
        }
    }
}
