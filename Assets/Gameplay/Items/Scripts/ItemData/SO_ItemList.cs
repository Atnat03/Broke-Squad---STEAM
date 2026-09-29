using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemData
{
    [CreateAssetMenu(fileName = "ItemList", menuName = "Item/ItemList", order = 1)]
    public class SO_ItemList : ScriptableObject
    {
        [SerializeField] private List<SO_Item> _itemDataList = new  List<SO_Item>();

        public SO_Item GetItem(int index)
        {
            return _itemDataList.Find(x => x.id == index);
        }
    }
}