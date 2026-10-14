using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class GoalModule : ItemModule, IPassif, IServerAction
    {
        public void OnCollide(Collision collision, ItemPickup item) { }

        public void OnThrow(Rigidbody rb) { }

        public void OnStartHolding()
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                ValueB = true
            });
        }

        public void OnStopHolding()
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                ValueB = false
            });
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            if (data.ValueB)
            {
                Context.Inventory.InvokeEvent(new OnGrabGoal_EVENT());
            }
            else
            {
                Context.Inventory.InvokeEvent(new OnDropGoal_EVENT());
            }
        }
    }
}