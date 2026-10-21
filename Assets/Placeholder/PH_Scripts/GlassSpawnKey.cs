using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

public class GlassSpawnKey : NetworkBehaviour
{
    [SerializeField] private SO_Item _item;
    
    public void SpawnObject()
    {
        ItemPickup n = Instantiate(_item.PickupPrefab,  transform.position, transform.rotation).GetComponent<ItemPickup>();
        n.NetworkObject.Spawn();
        n.Setup(new ItemInstance(_item));
    }
}
