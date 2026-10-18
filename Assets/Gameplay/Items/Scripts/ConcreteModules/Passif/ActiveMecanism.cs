using System;
using Gameplay.LD.Proto1.Scripts;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ActiveMecanism : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocity = 1;
        
        private Rigidbody _rb;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (_rb == null && !item.TryGetComponent(out _rb)) return;

            float speed = collision.impulse.magnitude / _rb.mass;
            
            ABPrint.Print("Speed : " + speed);

            if (speed <= _minVelocity) return;

            if (collision.collider.TryGetComponent(out ActivatedByItem ld))
            {
                ld.TriggerActivation();
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