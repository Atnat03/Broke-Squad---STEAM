/*using System;
using System.Collections.Generic;
using System.Linq;
using Bus;
using Gameplay.Items.Scripts;
using Gameplay.Items.Scripts.ItemModules.ConcreteModules;
using Gameplay.Items.Scripts.PlayerItemGestion;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public class PriseElectrique : NetworkBusListener
    {
        [SerializeField] private float _chargeDistance = 10;
        [SerializeField] private float _chargePerSeconde = 10;
        [SerializeField, SoundName] private string _plugSound;

        private void Awake()
        {
            GetComponent<SphereCollider>().radius = _chargeDistance;
        }

        public void OnTriggerStay(Collider collision)
        {
            if (collision.TryGetComponent(out ItemPickup item))
            {
                if(item == null)
                    return;
                
                ElectricModule elect = item.Instance.Conditions.OfType<ElectricModule>().FirstOrDefault();

                if (elect != null)
                    elect.AddEnergy(_chargePerSeconde * Time.deltaTime);
            }
        }

        public void OnTriggerEnter(Collider collision)
        {
            if (!IsServer) return;
            
            if (collision.TryGetComponent(out ItemPickup item))
            {
                if(item == null)
                    return;
                
                ElectricModule elect = item.Instance.Conditions.OfType<ElectricModule>().FirstOrDefault();

                if (elect != null)
                {
                    ReplicatePlugSoundRpc();
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ReplicatePlugSoundRpc()
        {
            InvokeEvent(new PlaySoundEvent
            {
                soundName = _plugSound,
                position = transform.position,
                volume = 0.3f
            });
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _chargeDistance);
        }
    }
}*/