using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public abstract class LDEffectModule : LDElementModule
    {
        public enum EffectTarget
        {
            Server,
            AllClients,
        }

        protected override void OnInitialize(ModuleConfig config)
        {
            
        }

        public abstract void Activate(LDActivation activation);

        public virtual void Stop()
        {

        }

        public virtual EffectTarget RunsOn => EffectTarget.Server;
    }
}
