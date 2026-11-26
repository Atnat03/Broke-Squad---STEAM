using Gameplay.IA;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public class LockDownEffect : LDEffectModule
    {
        public override void Activate(LDActivation activation)
        {
            InvokeEvent(new OnLockDownStart_EVENT
            {
                PlayerTriggerName = activation.TriggerPlayerName
            });
        }
    }
}
