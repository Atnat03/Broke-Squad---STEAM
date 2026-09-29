using Gameplay.Items.Scripts.ItemData;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] int _id = 0;
        [SerializeField] SO_ItemList _itemDataList;
        
        private ItemInstance _instance;

        public ItemInstance Instance
        {
            get
            {
                if (_instance == null && _itemDataList != null)
                    _instance = new ItemInstance(_itemDataList.GetItem(_id));

                return _instance;
            }
        }
        
        public void Setup(ItemInstance instance) => _instance = instance;
    }
}