using System.Collections.Generic;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemInstance
    {
        public SO_Item Data { get; }
        public List<ILeftClick> LeftClicks { get; }
        public List<IRightClick> RightClicks { get; }
        public List<ICondition> Conditions { get; }
        public List<IPassif> Passifs { get; }

        public ItemInstance(SO_Item data)
        {
            Data = data;
            LeftClicks  = new List<ILeftClick>(data.leftClicksActions);
            RightClicks = new List<IRightClick>(data.rightClicksActions);
            Conditions  = new List<ICondition>(data.conditions);
            Passifs = new List<IPassif>(data.passif);

            foreach (var m in AllModules())
                m?.ResetState();
        }

        public IEnumerable<IItemModule> AllModules()
        {
            foreach (var m in LeftClicks)  yield return m;
            foreach (var m in RightClicks) yield return m;
            foreach (var m in Conditions)  yield return m;
            foreach (var m in Passifs)     yield return m;
        }
    }
}