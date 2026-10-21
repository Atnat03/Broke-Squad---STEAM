using Bus;
using Gameplay.Controller;
using Gameplay.IA;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class Laser : NetworkBusListener
    {
        public void OnTriggerEnter(Collider other)
        {
            if(!IsServer) return;

            ThrowEventClientRpc(other.transform.GetComponent<PlayerController>().OwnerClientId);
        }

        [Rpc(SendTo.Everyone)]
        private void ThrowEventClientRpc(ulong clientID)
        {
            InvokeEvent(new OnLockDownStart_EVENT
            {
                ClientID = clientID
            });
        }
    }
}