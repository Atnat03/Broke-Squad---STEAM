using System;
using Gameplay.LD.Scripts;
using Gameplay.PlayerData;
using MyPrint;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class DamageWithVelocityModule : ItemModule, IPassif
    {
        public override string Emoji => "🤕";
        
        [SerializeField] private float _minVelocityForDamage = 1;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private bool _canDamagePlayer = true;
        [SerializeField] private bool _destroyOnApplyDamage = false;
        
        private Rigidbody _rb;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (_rb == null && !item.TryGetComponent(out _rb)) return;
            if(_rb.linearVelocity.magnitude < 1f) return;
            
            float speed = collision.relativeVelocity.magnitude;
            
            IDamageable damageable = collision.gameObject.GetComponentInChildren<IDamageable>() ?? collision.gameObject.GetComponent<IDamageable>();
            
            if (damageable != null)
            {
                ABPrint.Print("Speed :"  + speed + " || min speed : " + _minVelocityForDamage + " || rb velocity : " + _rb.linearVelocity.magnitude);
                
                if (speed <= _minVelocityForDamage) return;
               
                if (collision.gameObject.GetComponentInChildren<PlayerHealth>())
                {
                    if (!_canDamagePlayer)
                        return;
                }
                
                damageable.ApplyDamage(_damage);
            }
            
            if (_destroyOnApplyDamage)
            {
                if(item)
                    item.NetworkObject.Despawn();
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