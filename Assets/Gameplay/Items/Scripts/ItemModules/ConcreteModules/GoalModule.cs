using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public struct OnGrabGoal
    { }
    
    public struct OnDropGoal
    { }
    
    public class GoalModule : ItemModule, IPassif
    {
        public void OnCollide(Collision collision, ItemPickup item)
        { }

        public void OnThrow(Rigidbody rb)
        { }

        public void OnStartHolding()
        {
            Context.Inventory.TryNotifyGoal(true);
        }

        public void OnStopHolding()
        {
            Context.Inventory.TryNotifyGoal(false);
        }
    }
}