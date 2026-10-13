using System;

namespace Gameplay.LD
{
    [Serializable]
    public class FlagConditionConfig : ModuleConfig
    {
        public LDFlag Flag = LDFlag.Armed;
        public bool Required = true;
        public override Type ModuleType => typeof(FlagCondition);
    }
}