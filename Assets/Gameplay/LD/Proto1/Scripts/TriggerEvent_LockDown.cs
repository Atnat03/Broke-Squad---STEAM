using Bus;
using Gameplay.IA;
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
            ListenToEvent<OnLockDownStart_EVENT>(StartLockDown);
            ListenToEvent<OnLockDownEnd_EVENT>(StopLockDown);
        }
        
        private void StartLockDown(OnLockDownStart_EVENT data)
        {
            _startLockDownEvent?.Invoke();
        }
        
        private void StopLockDown(OnLockDownEnd_EVENT data)
        {
            _endLockDownEvent?.Invoke();
        }
    }
}