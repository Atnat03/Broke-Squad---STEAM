using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public abstract class LDConditionModule : LDElementModule
    {
        public abstract bool CanActivate(LDActivation activation);
    }
}
