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
    }
}
