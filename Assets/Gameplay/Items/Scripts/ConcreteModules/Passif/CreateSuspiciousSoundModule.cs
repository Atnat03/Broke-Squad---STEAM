using System;
using Gameplay.Other.SuspiciousSound;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class CreateSuspiciousSoundModule : ItemModule, IPassif
    {
        [SerializeField] private float _miniVelocityToDoSound = 1;
        [SerializeField] private SO_SuspiciousSoundSettings _suspiciousSoundSettings;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = collision.relativeVelocity.magnitude;
            
            if (speed <= _miniVelocityToDoSound) return;
            
            Context.SendSuspiciousSound(Context.Inventory.OwnerClientId, item.transform.position, _suspiciousSoundSettings);
        }

        public void OnThrow(Rigidbody rb)
        { }

        public void OnStartHolding()
        { }

        public void OnStopHolding()
        { }
    }
}