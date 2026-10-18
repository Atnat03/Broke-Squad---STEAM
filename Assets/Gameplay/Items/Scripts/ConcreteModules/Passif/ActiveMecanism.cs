using System;
using Gameplay.LD.Proto1.Scripts;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ActiveMecanism : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocity = 1;
        
        private Vector3 _lastVelocity;

        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = Mathf.Abs(Vector3.Dot(_lastVelocity, collision.GetContact(0).normal));

            if (speed <= _minVelocity) return;

            if (collision.collider.TryGetComponent(out ActivatedByItem ld))
            {
                ld.TriggerActivation();
            }
        }

        public void OnThrow(Rigidbody rb)
        {
            _lastVelocity = rb.linearVelocity;
        }

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