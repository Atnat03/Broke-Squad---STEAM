using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class GoalModule : ItemModule, IPassif
    {
        public void OnCollide(Collision collision, ItemPickup item) { }

        public void OnThrow(Rigidbody rb) { }

        public void OnStartHolding()
        {
            Context.SendEventToEveryone(new OnModuleDoAction_EVENT
            {
                ValueB = true
            });
        }

        public void OnStopHolding()
        {
            Context.SendEventToEveryone(new OnModuleDoAction_EVENT
            {
                ValueB = false
            });
        }
    }
}