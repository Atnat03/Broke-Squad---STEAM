using System;
using Assets.Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class BreakWithVelocity : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForDestroy = 1;
        [SerializeField] private float _force = 10;

        private Vector3 _lastVelocity;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = Mathf.Abs(Vector3.Dot(_lastVelocity, collision.GetContact(0).normal));
            
            if (speed <= _minVelocityForDestroy) return;
            
            if (item.TryGetComponent(out BrokenWindow obj))
            {
                obj.ApplyDamage(_force);
            }
        }

        public void OnThrow(Rigidbody rb)
        { }
        
        public void OnFixedUpdate(Rigidbody rb)
        {
            _lastVelocity = rb.linearVelocity;
        }

        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}