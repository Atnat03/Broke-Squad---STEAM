using System.Collections.Generic;
using Gameplay.Items.Scripts.ItemData;
using Gameplay.Items.Scripts.ItemModules;
using UnityEngine;

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
            LeftClicks  = CloneAll(data.leftClicksActions);
            RightClicks = CloneAll(data.rightClicksActions);
            Conditions  = CloneAll(data.conditions);
            Passifs     = CloneAll(data.passif);

            foreach (var m in AllModules())
                m?.ResetState();
        }

        private ItemInstance(ItemInstance source)
        {
            Data = source.Data;
            LeftClicks  = CloneAll(source.LeftClicks);
            RightClicks = CloneAll(source.RightClicks);
            Conditions  = CloneAll(source.Conditions);
            Passifs     = CloneAll(source.Passifs);
        }

        public ItemInstance Clone() => new ItemInstance(this);

        private static List<T> CloneAll<T>(IEnumerable<T> source) where T : class
        {
            var list = new List<T>();
            if (source == null) return list;

            foreach (var m in source)
            {
                if (m == null) continue;

                if (m is ScriptableObject so)
                    list.Add(Object.Instantiate(so) as T);
                else if (m is IItemModule im)
                    list.Add(im.Clone() as T);
                else
                    list.Add(m);
            }
            return list;
        }

        public void Cleanup()
        {
            foreach (var m in AllModules())
                if (m is Object o && o != null)
                    Object.Destroy(o);
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