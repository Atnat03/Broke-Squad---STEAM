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
            Triggered?.Invoke(activation);
        }
    }
}
