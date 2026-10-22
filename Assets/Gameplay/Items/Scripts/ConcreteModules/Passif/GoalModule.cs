using System;
using Gameplay.IA;
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
                ValueB = true,
                ValueS = Context.Inventory.Controller.PlayerName
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
                LockDownManager.instance.StartLockDown(data.ValueS, true);
                Context.Inventory.InvokeEvent(new OnGrabGoal_EVENT());
            }
            else
            {
                Context.Inventory.InvokeEvent(new OnDropGoal_EVENT());
            }
        }
    }
}