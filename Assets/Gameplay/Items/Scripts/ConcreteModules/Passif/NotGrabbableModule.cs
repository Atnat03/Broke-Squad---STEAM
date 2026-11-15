using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class NotGrabbableModule : ItemModule, IPassif
    {
        public override string Emoji => "🚫";

        
        public void OnCollide(Collision collision, ItemPickup item) { }

        public void OnThrow(Rigidbody rb) { }

        public void OnStartHolding() { }

        public void OnStopHolding()
        { }
    }
}