using Bus;
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
            
            InvokeEvent(new OnLockDownStart_EVENT());
        }
    }
}