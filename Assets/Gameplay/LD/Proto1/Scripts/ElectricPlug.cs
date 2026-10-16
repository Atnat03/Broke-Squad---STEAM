using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

public class ElectricPlug : NetworkBehaviour
{
    [SerializeField] private float _percentageGainPerSecond = 5;

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        ItemPickup pickup = other.GetComponentInParent<ItemPickup>();
        if (pickup == null) return;

        float gain = _percentageGainPerSecond * Time.fixedDeltaTime;

        foreach (IItemModule m in pickup.Instance.AllModules())
        {
            if (m is ElectricUseModule elec)
                elec.AddEnergy(gain);
        }
    }
}
