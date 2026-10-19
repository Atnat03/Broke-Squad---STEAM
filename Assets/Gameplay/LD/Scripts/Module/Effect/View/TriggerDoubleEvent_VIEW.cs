using Bus;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD
{
    public class TriggerDoubleEvent_VIEW : NetworkBusListener
    {
        [Header("KEY")]
        [SerializeField] private string _keyEvent = "LD_TRIGGER_EVENT";
        
        [Header("Events")]
        [SerializeField] private UnityEvent[] _onOpenEvent;
        [SerializeField] private UnityEvent[] _onCloseEvent;
        
        public override void OnNetworkSpawn()
        {
            ListenToEvent<On_LD_TriggerEventEffect_EVENT>(OnTriggerEvent);
        }

        private void OnTriggerEvent(On_LD_TriggerEventEffect_EVENT data)
        {
            if (data.Key != _keyEvent) return;
            
            if(!IsServer) return;

            TriggerClientRpc(data.state);
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerClientRpc(bool dataState)
        {
            if (dataState)
            {
                foreach (UnityEvent e in _onOpenEvent)
                {
                    e.Invoke();
                }
            }
            else
            {
                            
                foreach (UnityEvent e in _onCloseEvent)
                {
                    e.Invoke();
                }
            }
        }
    }
}