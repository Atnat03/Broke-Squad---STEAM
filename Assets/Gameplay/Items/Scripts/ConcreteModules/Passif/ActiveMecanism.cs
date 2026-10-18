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
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = collision.relativeVelocity.magnitude;
            
            if (speed <= _minVelocity) return;
            
            if (collision.collider.TryGetComponent(out ActivatedByItem ld))
            {
                ld.TriggerActivation();
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