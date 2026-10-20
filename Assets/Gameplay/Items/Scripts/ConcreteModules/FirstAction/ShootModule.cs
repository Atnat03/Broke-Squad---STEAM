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
        [SerializeField] private Vector3 _shootPos_SECURITY;
        [SerializeField] private float _shootForce;
        
        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
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

            Vector3 pos = _shootPos_SECURITY;
            
            if(Context.Core.transform.childCount > 0)
            {
                pos = Context.Core.transform.GetChild(0).position;
            }
            
            Vector3 origin = pos;
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
            
            ItemInstance item = new ItemInstance(_ammoPrefab);
            
            if (item == null || item.Data.PickupPrefab == null)
                return;
            
            if (ammo.TryGetComponent(out ItemPickup pickup))
            {
                ABPrint.Print(item.Data.ItemName);
                pickup.Setup(item);
            }
            
            if (ammo.TryGetComponent(out Rigidbody rb))
            {
                foreach (IPassif p in item.Passifs)
                {
                    p.OnThrow(rb);
                }
                
                rb.useGravity = false;
                pickup.OnCollide += () => rb.useGravity = true;
                
                rb.AddForce(data.Direction * data.ValueF, ForceMode.Impulse);
            }
            
            //SFX
            Context.Inventory.InvokeEvent(new PlaySoundEvent
            {
                position = pickup.transform.position,
                volume = _volume,
                soundName = _stringName
            });
        }
    }
}