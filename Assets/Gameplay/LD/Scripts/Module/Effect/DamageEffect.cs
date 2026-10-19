using Gameplay.LD.Scripts;
using UnityEngine;

namespace Gameplay.LD
{
    public class DamageEffect : LDEffectModule
    {
        private float _damage;

        protected override void OnInitialize(ModuleConfig config) => _damage = ((DamageEffectConfig)config).Damage;

        public override void Activate(LDActivation activation)
        {
            if (!activation.TryGetInstigator(out var instigator)) 
            { 
                Debug.LogWarning($"[LD] Activation without instigator", this);
                return;
            }

            IDamageable target = instigator.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                Debug.LogWarning($"[LD] {instigator.name} n'implemente pas IDamageable", this);
                return;
            }
            target.ApplyDamage(_damage);
        }
    }
}