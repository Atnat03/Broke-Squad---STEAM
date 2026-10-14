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

            Transform cam = Context.Camera.transform;

            Ray ray = new Ray(cam.position, cam.forward);

            Vector3 aimPoint = ray.origin + ray.direction * 100f;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                aimPoint = hit.point;
            }

            Vector3 origin = cam.TransformPoint(_shootPos);
            Vector3 direction = (aimPoint - origin).normalized;
            
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
            Quaternion rotation = Quaternion.LookRotation(data.Direction);
            
            NetworkObject ammo = Context.SpawnGameObject(_ammoPrefab.PickupPrefab.gameObject, data.Position, rotation);

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