using System.Collections.Generic;
using Gameplay.Items;

namespace Gameplay.LD
{
    public class ConsumeItemEffect : LDEffectModule
    {
        public override void Activate(LDActivation activation)
        {
            if (!activation.TryGetInstigator(out var player)) return;

            PlayerInventory inventory = player.GetComponentInChildren<PlayerInventory>();
            if (inventory == null) return;

            ItemInstance currentItem = inventory.GetCurrentItemInHand();
            if (currentItem == null) return;

            HashSet<ICondition> done = new HashSet<ICondition>();
            foreach (IFirstAction action in currentItem.FirstActionList)
            {
                ICondition condition = action.Condition;
                if (condition != null && done.Add(condition))
                    condition.UseItem();
            }
        }
    }
}
