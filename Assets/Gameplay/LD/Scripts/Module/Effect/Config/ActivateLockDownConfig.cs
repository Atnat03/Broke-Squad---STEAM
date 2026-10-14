using System;

namespace Gameplay.LD
{
    [Serializable]
    public class ActivateLockDownConfig : ModuleConfig
    {
        public override Type ModuleType => typeof(ActivateLockDownEffect);
    }
}