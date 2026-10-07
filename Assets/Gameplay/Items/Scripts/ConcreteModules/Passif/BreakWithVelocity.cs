using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class BreakWithVelocity : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForDestroy = 1;

        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = collision.relativeVelocity.magnitude;
            
            if (speed <= _minVelocityForDestroy) return;
            
            if (item.TryGetComponent<NetworkObject>(out var obj))
            {
                obj.Despawn();
            }
        }

        public void OnThrow(Rigidbody rb)
        { }

        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}