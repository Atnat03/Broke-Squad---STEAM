using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    [RequireComponent(typeof(Collider))]
    public class PlayerEnterTrigger : LDTriggerModule
    {
        protected override void OnInitialize()
        {
            GetComponent<Collider>().isTrigger = true;
            if (!TryGetComponent<Rigidbody>(out _))
                gameObject.AddComponent<Rigidbody>().isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Context.IsServer) return;
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsPlayerObject) return;
            Raise(new LDActivation(netObj, netObj.transform.position));
        }
    }
}