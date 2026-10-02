using System;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ChargeItemModule : ItemModule, ILeftClick
    {
        [SerializeField] private float _amountPercentChargePerClick = 25;
        [SerializeField] private float _range = 2f;
        [SerializeField, SoundName] private string _chargeSound;

        public float Range => _range;

        public void StartLeftClick()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _range)
                && hit.transform.TryGetComponent(out ItemPickup pickup)
                && pickup.TryGetComponent(out NetworkObject netObj))
            {
                Context.Inventory.TryPlaySound(_chargeSound);
                Context.Inventory.RequestChargePickup(netObj);
                Context.Core.UseCondition();
            }
        }

        public void EndLeftClick() { }

        public void ApplyCharge(ItemPickup pickup)
        {
            if (pickup.Instance == null) return;

            foreach (ICondition c in pickup.Instance.Conditions)
                if (c is ElectricModule electric)
                    electric.AddEnergy(_amountPercentChargePerClick);
        }
    }
}