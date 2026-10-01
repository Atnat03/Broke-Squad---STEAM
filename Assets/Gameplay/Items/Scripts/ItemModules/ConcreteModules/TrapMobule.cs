using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class TrapMobule : ItemModule, IPassif
    {
        private bool _isAmorced = false;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            
        }

        public void OnThrow(Rigidbody rb)
        {
            
        }

        public void OnUpdateState()
        {
            
        }
    }
}