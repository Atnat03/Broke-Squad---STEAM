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
        [SerializeField] private float _weight = 1;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damage))
            {
                float speed = collision.relativeVelocity.magnitude;
                
                ABPrint.Print("Speed : " + speed, ABColor.Purple);
                
                if (speed <= _minVelocityForDamage) return;

                float t = Mathf.InverseLerp(_minVelocityForDamage, _maxVelocity, speed);
                
                float speedFactor = _damageCurve.Evaluate(t);
                
                float dmg = _maxDamage * t * speedFactor;
                
                ABPrint.Print("Speed : " + speed + " / Dmg : " + dmg, ABColor.Purple);
                
                damage.ApplyDamage(dmg);
            }
        }

        public void OnThrow(Rigidbody rb)
        {
            rb.mass = _weight;
        }

        public void OnUpdateState()
        { }
    }
}