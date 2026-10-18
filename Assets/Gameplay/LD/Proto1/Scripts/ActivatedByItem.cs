using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD.Proto1.Scripts
{
    public class ActivatedByItem : NetworkBehaviour
    {
        [SerializeField] private UnityEvent _triggerEvent;

        public void TriggerActivation()
        {
            if (!IsServer)
            {
                TriggerActivationServerRpc();
                return;
            }
            
            TriggerActivationClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerActivationClientRpc()
        {
            _triggerEvent?.Invoke();
        }
        

        [Rpc(SendTo.Server)]
        private void TriggerActivationServerRpc() => TriggerActivation();
    }
}