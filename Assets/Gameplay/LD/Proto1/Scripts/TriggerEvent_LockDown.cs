using Bus;
using Gameplay.IA;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD.Proto1
{
    public class TriggerEvent_LockDown : NetworkBusListener
    {
        [SerializeField] private UnityEvent _startLockDownEvent;
        [SerializeField] private UnityEvent _endLockDownEvent;

        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                ListenToEvent<OnLockDownStart_EVENT>(StartLockDown);
                ListenToEvent<OnLockDownEnd_EVENT>(StopLockDown);
            }
        }
        
        private void StartLockDown(OnLockDownStart_EVENT data)
        {
            TriggerStartLockDownEventRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerStartLockDownEventRpc()
        {
            _startLockDownEvent?.Invoke();
        }
        
        private void StopLockDown(OnLockDownEnd_EVENT data)
        {
            TriggerEndLockDownEventRpc();
        }
        
        [Rpc(SendTo.Everyone)]
        private void TriggerEndLockDownEventRpc()
        {
            _endLockDownEvent?.Invoke();
        }
    }
}