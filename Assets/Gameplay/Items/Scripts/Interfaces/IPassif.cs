using UnityEngine;

namespace Gameplay.Items
{
    public interface IPassif : IItemModule
    {
        public void OnCollide(Collision collision, ItemPickup item);
        public void OnThrow(Rigidbody rb);
        public void OnStartHolding();
        public void OnStopHolding();
    }
}