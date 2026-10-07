using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public abstract class LDEffectModule : LDElementModule
    {
        protected override void OnInitialize(ModuleConfig config)
        {
            throw new NotImplementedException();
        }

        public abstract void Activate(LDActivation activation);

        public virtual void Stop()
        {

        }
    }
}
