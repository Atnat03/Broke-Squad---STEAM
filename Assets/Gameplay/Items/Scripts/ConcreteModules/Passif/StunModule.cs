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
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (collision.gameObject.TryGetComponent<IStunnable>(out var stun))
            {
                stun.ApplyStun(_stunDuration);
            }        
        }

        public void OnThrow(Rigidbody rb) { }
        public void OnStartHolding() { }
        public void OnStopHolding() { }
    }
}