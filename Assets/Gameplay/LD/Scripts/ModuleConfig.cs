using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    [Serializable]
    public abstract class ModuleConfig
    {
        public int ModuleID;
        public abstract Type ModuleType { get; }

        public override void OnInitialize(LDElementModule module)
        {
            if (module is LogMsgEffect logMsgEffect)
            {
                logMsgEffect._message = Message;
            }
            else
            {
                throw new InvalidOperationException($"Module is not of type {nameof(LogMsgEffect)}");
            }
        }
    }
}
