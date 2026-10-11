using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class PlayerEnterTrigger : LDTriggerModule
    {
        private Vector3 _center;
        private Vector3 _size;
        protected override void OnInitialize(ModuleConfig config)
        {
            PlayerEnterTriggerConfig playerEnterTriggerConfig = (PlayerEnterTriggerConfig)config;
            _center = playerEnterTriggerConfig.Center;
            _size = playerEnterTriggerConfig.Size;

            if (!Context.IsServer) return;

            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = _center;
            trigger.size = _size;

            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Context.IsServer) return;

            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if(netObj == null || !netObj.IsPlayerObject) return;

            Raise(new LDActivation(netObj, netObj.transform.position));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_center, _size);
        }
    }
}