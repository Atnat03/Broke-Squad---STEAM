using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

public class ElectricPlug : NetworkBehaviour
{
    [SerializeField] private float _percentageGainPerSecond = 5;
    
    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        
        float gain = _percentageGainPerSecond * Time.fixedDeltaTime;

        PlayerInventory player = other.GetComponentInChildren<PlayerInventory>();
        
        ItemPickup pickup = other.GetComponentInParent<ItemPickup>();

        if (pickup != null)
        {
            if (pickup.Instance == null) return;
            
            foreach (IItemModule m in pickup.Instance.AllModules())
            {
                if (m is ElectricUseModule elec)
                    elec.AddEnergy(gain);
            }
        }

        if (player != null)
        {
            if(player.GetCurrentItemInHand() == null) return;
            
            foreach (IItemModule m in player.GetCurrentItemInHand().AllModules())
            {
                if (m is ElectricUseModule elec)
                    elec.AddEnergy(gain);
            }
        }
    }
}
