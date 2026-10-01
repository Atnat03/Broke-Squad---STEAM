using System;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Gameplay.LD.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class DestroyWithVelocity : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForDestroy = 1;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = collision.relativeVelocity.magnitude;
            
            ABPrint.Print(speed.ToString(), ABColor.Green);
            
            if (speed <= _minVelocityForDestroy) return;

            if (item.TryGetComponent<NetworkObject>(out var obj))
            {
                obj.Despawn();
            }
        }

        public void OnThrow(Rigidbody rb)
        { }

        public void OnUpdateState()
        { }
    }
}