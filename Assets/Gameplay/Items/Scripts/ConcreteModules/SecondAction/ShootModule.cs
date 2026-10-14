using System;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ShootModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] private SO_Item _ammoPrefab;
        [SerializeField] private Vector3 _shootPos;
        [SerializeField] private float _shootForce;
        
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            if (!CanUse()) return;

            Vector3 origin = Context.Camera.transform.position + _shootPos;
            Vector3 direction = Context.Camera.transform.forward;
            
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                Position = origin,
                Direction = direction,
                ValueF = _shootForce
            });
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction()
        { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            NetworkObject ammo = Context.SpawnGameObject(_ammoPrefab.PickupPrefab.gameObject, data.Position, _ammoPrefab.PickupPrefab.transform.rotation);

            ItemInstance item = Context.Inventory.GetCurrentItemInHand();
            
            if (item == null || item.Data.PickupPrefab == null)
                return;
            
            if (ammo.TryGetComponent(out ItemPickup pickup))
            {
                pickup.Setup(item);
            }
            
            ABPrint.Print("Spawn new ammo at : " + data.Position, ABColor.Green);
            
            if (ammo.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(data.Direction * data.ValueF, ForceMode.Impulse);
                ABPrint.Print("Impulse new ammo", ABColor.Green);
            }
        }
    }
}