using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Items.Scripts.ItemModules.ConcreteModules;
using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public class PriseElectrique : MonoBehaviour
    {
        [SerializeField] private float _chargeDistance = 10;
        [SerializeField] private float _chargePerSeconde = 10;

        private void Awake()
        {
            GetComponent<SphereCollider>().radius = _chargeDistance;
        }

        public void OnTriggerStay(Collider collision)
        {
            if (collision.TryGetComponent(out PlayerInventory inventory))
            {
                if(inventory == null)
                    return;
                
                ElectricModule elect = inventory.CurrentItem.Instance.Conditions.OfType<ElectricModule>().FirstOrDefault();

                if (elect != null)
                    elect.AddEnergy(_chargePerSeconde * Time.deltaTime);
            }
        }
        
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _chargeDistance);
        }
    }
}