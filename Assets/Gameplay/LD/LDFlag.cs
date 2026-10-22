using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    [Flags]
    public enum LDFlag
    {
        None = 0,
        Armed = 1 << 0,
        Activated = 1 << 1,
        Triggered = 1 << 2,
        Active = 1 << 3,
        Locked = 1 << 4,
        OpenFront = 1 << 5,
        OpenBack = 1 << 6
    }
}
