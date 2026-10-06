using System.Collections.Generic;

namespace Gameplay.Items
{
    public interface ISecondAction : IItemModule
    {
        public List<ICondition> Conditions { get; }
        
        public void StartSecondAction();
        public void StopSecondAction();
    }
}