using System;
using Gameplay.Items.Scripts.PlayerItemGestion;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ChargeItemModule : ItemModule, ILeftClick
    {
        [SerializeField] private float _amountPercentChargePerClick = 25;
        
        public void StartLeftClick()
        {
            RaycastHit hit;
            
            if (Physics.Raycast(Context.Camera.transform.position, Context.Camera.transform.forward,out hit, 2))
            {
                if (hit.transform.TryGetComponent(out ItemPickup pickup))
                {
                    foreach (ICondition condition in pickup.Instance.Conditions)
                    {
                        if (condition is ElectricModule electric)
                            electric.AddEnergy(_amountPercentChargePerClick);
                    }
                }
            }
        }

        public void EndLeftClick()
        { }
    }
}