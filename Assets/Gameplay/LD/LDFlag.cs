using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Gameplay.LD
{
    [Flags]
    public enum LDFlag
    {
        None = 0,
        Armed = 1 << 0,
        Activated = 1 << 1,
        Triggered = 1 << 2,
        Active = 1 << 3,
    }
}
