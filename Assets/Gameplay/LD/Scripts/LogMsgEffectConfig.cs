using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    [Serializable]
    public class LogMsgEffectConfig : ModuleConfig
    {
        public string Message;
        public override Type ModuleType => typeof(LogMsgEffect);

    }
}
