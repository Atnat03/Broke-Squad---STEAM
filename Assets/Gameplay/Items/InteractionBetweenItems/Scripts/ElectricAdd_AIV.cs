using System;
using Gameplay.LD;
using MyPrint;

namespace Gameplay.Items
{
    [Serializable]
    public class ElectricAdd_AIV : IActionInInventory
    {
        public void DoAction(ItemInstance myself, ItemInstance otherItem)
        {
            ElectricUseModule electricUseModule = null;
            ChargeItemModule chargeItemModule = null;
            
            foreach (var f in myself.FirstActionList)
                if (f.Condition is ElectricUseModule)
                    electricUseModule = (ElectricUseModule)f.Condition;

            foreach (var f in otherItem.FirstActionList)
                if (f is ChargeItemModule)
                    chargeItemModule = (ChargeItemModule)f;

            if (electricUseModule == null || chargeItemModule == null) return;
            
            if(electricUseModule.AddEnergy(chargeItemModule.PercentGain))
            {
                chargeItemModule.Condition.UseItem();
            }
        }
    }
}