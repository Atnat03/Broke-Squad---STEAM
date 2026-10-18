using System;
using Gameplay.IA.Scripts;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class StunModule : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForStun = 2;
        [SerializeField] private float _stunDuration = 1f;
        
        private Vector3 _lastVelocity;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = Mathf.Abs(Vector3.Dot(_lastVelocity, collision.GetContact(0).normal));

            if(speed < _minVelocityForStun) return;
            
            if (collision.gameObject.TryGetComponent<IStunnable>(out var stun))
            {
                stun.ApplyStun(_stunDuration);
            }        
        }

        public void OnThrow(Rigidbody rb) { }
        public void OnStartHolding() { }
        public void OnStopHolding() { }
        
        public void OnFixedUpdate(Rigidbody rb)
        {
            _lastVelocity = rb.linearVelocity;
        }
    }
}