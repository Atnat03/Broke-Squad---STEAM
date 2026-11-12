using System;
using System.Collections.Generic;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.LD
{
    [RequireComponent(typeof(Collider))]
    public class ElectricChargeEffect : LDEffectModule
    {
        [SerializeField, Min(0f)] private float _chargeAmountPerSecond = 5f;

        protected override void OnInitialize() => GetComponent<Collider>().isTrigger = true;

        public override void Activate(LDActivation activation)
        {
            
        }

        private void OnTriggerStay(Collider other)
        {
            if (Context == null || !Context.IsServer) return;

            float gain = _chargeAmountPerSecond * Time.fixedDeltaTime;

            ItemPickup pickup = other.GetComponentInParent<ItemPickup>();
            if(pickup != null && pickup.Instance != null)
            {
                Charge(pickup.Instance.AllModules(), gain);
            }
        }

        private void Charge(IEnumerable<IItemModule> modules, float gain)
        {
            foreach (IItemModule m in modules)
            {
                if (m is ElectricUseModule elec)
                    elec.AddEnergy(gain);
            }
        }
    }
}
