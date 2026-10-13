using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public abstract class LDTriggerModule : LDElementModule
    {
        public event Action<LDActivation> Triggered;

        protected void Raise(LDActivation activation)
        {
            if (!Context.IsServer) return;
            Triggered?.Invoke(activation);
        }
    }
}
