using Bus;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD
{
    public class TriggerEvent_VIEW : NetworkBusListener
    {
        [Header("KEY")]
        [SerializeField] private string _keyEvent = "LD_TRIGGER_EVENT";
        
        [Header("Events")]
        [SerializeField] private UnityEvent[] _onTriggerEvent;
        
        public override void OnNetworkSpawn()
        {
            ListenToEvent<On_LD_TriggerEventEffect_EVENT>(OnTriggerEvent);
        }

        private void OnTriggerEvent(On_LD_TriggerEventEffect_EVENT data)
        {
            if (data.Key != _keyEvent) return;
            
            foreach (UnityEvent e in _onTriggerEvent)
            {
                e.Invoke();
            }
        }
    }
}