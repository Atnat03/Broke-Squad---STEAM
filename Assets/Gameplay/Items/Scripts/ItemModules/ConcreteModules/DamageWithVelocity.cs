using System;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Gameplay.LD.Scripts;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class DamageWithVelocity : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForDamage = 10;
        [SerializeField] private float _X = 10;
        [SerializeField] private float _weight = 1;

        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damage))
            {
                if (item.TryGetComponent(out Rigidbody rb))
                {
                    float dmg = ((rb.linearVelocity.magnitude - _minVelocityForDamage) * _weight) / _X;
                    ABPrint.Print(rb.linearVelocity.magnitude + " vitesse");
                    if (dmg <= 0) return;
                        
                    damage.ApplyDamage(dmg);
                }
            }
        }

        public void OnThrow()
        { }

        public void OnUpdateState()
        { }
    }
}