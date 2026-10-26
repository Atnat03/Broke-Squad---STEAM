using Bus;
using Gameplay.Controller;
using Gameplay.IA;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class Laser : NetworkBusListener
    {
        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (!other.TryGetComponent(out PlayerController p))
            {
                ABPrint.Print("Pas de controller", ABColor.Red);
                return;
            }

            LockDownManager.instance.StartLockDown(p.PlayerName);
        }
    }
}