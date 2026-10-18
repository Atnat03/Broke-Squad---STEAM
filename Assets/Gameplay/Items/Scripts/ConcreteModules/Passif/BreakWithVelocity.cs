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

        private Rigidbody _rb;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (_rb == null && !item.TryGetComponent(out _rb)) return;

            float speed = collision.impulse.magnitude / _rb.mass;
            
            if (speed <= _minVelocityForDestroy) return;
            
            if (item.TryGetComponent(out BrokenWindow obj))
            {
                obj.ApplyDamage(_force);
            }
        }

        public void OnThrow(Rigidbody rb)
        {
            _rb = rb;
        }

        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}