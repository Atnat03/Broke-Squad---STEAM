using System.Collections.Generic;

namespace Gameplay.Items
{
    public interface ISecondAction : IItemModule
    {
        public ICondition Condition { get; }
        
        public void StartSecondAction();
        public void StopSecondAction();
    }
}