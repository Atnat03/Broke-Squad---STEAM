using Gameplay.Items.Scripts.ItemData;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] int _id = 0;
        [SerializeField] SO_ItemList _itemDataList;
        [SerializeField] Outline _outline;

        private ItemInstance _instance;
        
        void Awake()
        {
            SetOutline(false);
        }
        
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

        public void SetOutline(bool state)
        {
            if(_outline !=  null)
                _outline.enabled = state;
        }

        public void OnCollisionEnter(Collision collision)
        {
            if(_instance == null)
                return;

            foreach (var passif in _instance.Passifs)
            {
                passif.OnCollide(collision, this);
            }
        }
    }
}