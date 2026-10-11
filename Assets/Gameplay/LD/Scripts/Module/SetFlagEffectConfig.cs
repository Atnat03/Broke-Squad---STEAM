using System;

namespace Gameplay.LD
{
    [Serializable]
    public class SetFlagEffectConfig : ModuleConfig
    {
        public LDFlag Flag = LDFlag.Armed;
        public bool Value = false;
        public override Type ModuleType => typeof(SetFlagEffect);
    }
}