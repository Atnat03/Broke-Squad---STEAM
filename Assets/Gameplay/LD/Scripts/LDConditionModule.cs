using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public abstract class LDConditionModule : LDElementModule
    {
        protected override void OnInitialize(ModuleConfig config)
        {
            throw new NotImplementedException();
        }

        public abstract bool CanActivate(LDActivation activation);
    }
}
