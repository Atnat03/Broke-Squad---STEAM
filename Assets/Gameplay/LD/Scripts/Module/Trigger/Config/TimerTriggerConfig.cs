using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    [Serializable]
    public class TimerTriggerConfig : ModuleConfig
    {
        public float Interval = 2f;
        public override Type ModuleType => typeof(TimerTrigger);
    }
}
