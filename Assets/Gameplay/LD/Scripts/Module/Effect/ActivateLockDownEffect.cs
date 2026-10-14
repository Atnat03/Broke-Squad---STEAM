using Gameplay.IA;

namespace Gameplay.LD
{
    public class ActivateLockDownEffect : LDEffectModule
    {
        public override void Activate(LDActivation activation)
        {
            if(!Context.IsServer) return;

            if (LockDownManager.instance == null) return;
            if (LockDownManager.instance.IsLockDown) return;
            
            InvokeEvent(new OnLockDownStart_EVENT());
        }
    }
}