using System;

namespace Gameplay.LD
{
    [Serializable]
    public class DamageEffectConfig : ModuleConfig
    {
        public float Damage = 10f;
        public override Type ModuleType => typeof(DamageEffect);
    }
}