using Unity.Collections;

namespace Gameplay.IA
{
    public struct OnLockDownStart_EVENT
    {
        public FixedString32Bytes PlayerTriggerName;
        public bool isGoal;
    }
    
    public struct OnLockDownEnd_EVENT{}
}