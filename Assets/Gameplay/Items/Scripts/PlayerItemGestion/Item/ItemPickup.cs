using Bus;
using UnityEngine;

namespace Gameplay.Items
{
    public class ItemPickup : NetworkBusListener, IInteractable
    {
        [SerializeField] private int _itemIndex = -1;
        [SerializeField] SO_ItemDataBase _itemDataList;
        [SerializeField] private Outline _outline;
        
        public Outline outline => _outline;
        
        private ItemInstance _instance;
        
        public ItemInstance Instance
        {
            get
            {
                if (_instance == null && _itemDataList != null)
                    _instance = new ItemInstance(_itemDataList.GetItemData(_itemIndex));

                return _instance;
            }
        }

        public override void OnNetworkSpawn()
        {
            SetOutline(false);
        }

        public void SetOutline(bool state)
        {
            outline.enabled = state;
        }

        public void Interact(ulong clientId)
        {
            InvokeEvent(new OnPickupItem_EVENT
            {
                ClientId = clientId,
                ItemPickUpRef = NetworkObject
            });
        }
        
        public void Setup(ItemInstance instance) => _instance = instance;
        
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