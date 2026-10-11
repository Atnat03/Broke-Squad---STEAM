using System.Collections.Generic;

namespace Gameplay.Items
{
    public interface IFirstAction : IItemModule
    {
        public ICondition Condition { get; }
        
        public void StartFirstAction();
        public void StopFirstAction();
    }
}