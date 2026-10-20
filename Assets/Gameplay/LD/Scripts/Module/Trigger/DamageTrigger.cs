using Gameplay.LD.Scripts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class DamageTrigger : LDTriggerModule, IDamageable
    {
        [SerializeField] private float _damageTreshold = 10f;

        public void ApplyDamage(float damage)
        {
            if (!Context.IsServer) return;
            if (damage < _damageTreshold) return;
            Raise(new LDActivation(null, transform.position));
        }

        protected override void OnInitialize()
        {
            
        }
    }
}
