using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Items
{
    public class ItemInstance
    {
        public SO_Item Data { get; }
        public List<IFirstAction> FirstActionList { get; }
        public List<ISecondAction> SecondActionList { get; }
        public List<IPassif> Passifs { get; }

        public ItemInstance(SO_Item data)
        {
            Data = data;
            FirstActionList = CloneAll(data.FirstActionList);
            SecondActionList = CloneAll(data.SecondActionList);
            Passifs = CloneAll(data.PassifList);

            foreach (var m in AllModules())
                m?.ResetState();
        }

        private ItemInstance(ItemInstance source)
        {
            Data = source.Data;
            FirstActionList = CloneAll(source.FirstActionList);
            SecondActionList = CloneAll(source.SecondActionList);
            Passifs = CloneAll(source.Passifs);
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

        public int IndexOf(IItemModule module)
        {
            int i = 0; 
            foreach (var m in AllModules())
            {
                if (ReferenceEquals(m, module)) return i;
                i++;
            }

            return -1;
        }

        public IItemModule GetModule(int index)
        {
            if (index < 0) return null;

            int i = 0;
            foreach (var m in AllModules())
            {
                if (i == index) return m;
                i++;
            }

            return null;
        }

        public IEnumerable<IItemModule> AllModules()
        {
            foreach (var m in FirstActionList) yield return m;
            foreach (var m in SecondActionList) yield return m;
            foreach (var m in Passifs) yield return m;
        }
    }
}