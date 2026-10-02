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
        [SerializeField] private float _minVelocityForDamage = 1;
        [SerializeField] private float _maxVelocity = 15f;
        [SerializeField] private float _maxDamage = 15f;
        [SerializeField] private AnimationCurve _damageCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _weightFactor = 1;
        [SerializeField] private float _mass = 1;
        [SerializeField] private bool _canDamagePlayer = false;
        [SerializeField] private float _minVelocityForSound = 3;
        [SerializeField, SoundName] private string _hitSound;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            float speed = collision.relativeVelocity.magnitude;

            if (speed >= _minVelocityForSound)
            {
                Context.Inventory.TryPlaySound(_hitSound);
            }
            
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damage))
            {
                ABPrint.Print("Speed : " + speed, ABColor.Purple);
                
                if (speed <= _minVelocityForDamage) return;

                float t = Mathf.InverseLerp(_minVelocityForDamage, _maxVelocity, speed);
                
                float speedFactor = _damageCurve.Evaluate(t);
                
                float dmg = _maxDamage * t * speedFactor * _weightFactor;
                
                ABPrint.Print("Speed : " + speed + " / Dmg : " + dmg, ABColor.Purple);

                if (collision.gameObject.TryGetComponent<PlayerData.PlayerData>(out var damageable))
                {
                    ABPrint.Print("Is a player", ABColor.Purple);
                    
                    if (!_canDamagePlayer)
                        return;
                }
                
                damage.ApplyDamage(dmg);
            }
        }

        public void OnThrow(Rigidbody rb)
        {
            rb.mass = _mass;
        }

        public void OnUpdateState()
        { }
    }
}