using Gameplay.LD.Scripts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class DamageTrigger : LDTriggerModule, IDamageable, IBreakable
    {
        [Tooltip("Seuil mini des dégats pour être compter")]
        [SerializeField] private float _damageTreshold = 10f;

        [Tooltip("PV de l'objet, 0 = se brise en 1 coup")]
        [SerializeField] private float _health = 20;

        private float _currentHealth;
        protected override void OnInitialize()
        {
            _currentHealth = _health;
        }

        public void ApplyDamage(float damage)
        {
            if (!Context.IsServer) return;
            if (damage < _damageTreshold) return;

            if (_health > 0)
            {
                if (_currentHealth <= 0f) return;
                _currentHealth -= damage;
                if (_currentHealth > 0f) return;
            }

            Debug.Log("[LD] damage detecté");

            Raise(new LDActivation(null, transform.position));
        }

        public void Break(float force) => ApplyDamage(force);
    }
}
