using System;

namespace Gameplay.LD
{
    [Serializable]
    public class TriggerEventEffectConfig : ModuleConfig
    {
        public string KeyEvent = "LD_TRIGGER_EVENT";
        
        public override Type ModuleType => typeof(TriggerEventEffect);
    }
}