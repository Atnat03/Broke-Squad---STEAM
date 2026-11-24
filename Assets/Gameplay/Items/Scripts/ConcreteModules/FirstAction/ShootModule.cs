using System;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ShootModule : ActionModule, IFirstAction, IServerAction
    {
        public override string Emoji => "🔫";
        
        [SerializeField] private SO_Item _ammoPrefab;
        [SerializeField] private Vector3 _shootPos_SECURITY;
        [SerializeField] private float _shootForce;
        [SerializeField] private LayerMask _aimMask = ~0;  
        [SerializeField] private float _aimDistance = 100f;
        
        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;

            Transform cam = Context.Camera.transform;

            Vector3 aimPoint = cam.position + cam.forward * _aimDistance;
            
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _aimDistance, _aimMask, QueryTriggerInteraction.Ignore))
                aimPoint = hit.point;

            Transform weaponTransform = Context.Core.transform;

            Vector3 muzzle = weaponTransform.position;

            if (weaponTransform.childCount > 0)
                muzzle = weaponTransform.GetChild(0).position;

            muzzle += weaponTransform.TransformVector(_shootPos_SECURITY);

            Vector3 origin = muzzle;
            
            Vector3 camToMuzzle = muzzle - cam.position;
            if (Physics.Raycast(cam.position, camToMuzzle.normalized, camToMuzzle.magnitude, _aimMask, QueryTriggerInteraction.Ignore))
                origin = cam.position;
            
            Vector3 direction = aimPoint - origin;
            if (direction.sqrMagnitude < 0.01f || Vector3.Dot(direction, cam.forward) <= 0f)
                direction = cam.forward;
            direction.Normalize();
            
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                Position = origin,
                Direction = direction,
                ValueF = _shootForce
            });

            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        private Vector3 CheckWall(Vector3 pos)
        {
            Vector3 finalPos = pos;
            Vector3 dir = Context.Camera.transform.forward;
            
            if (Physics.Raycast(pos, dir, out RaycastHit hit, 0.25f, ~0,  QueryTriggerInteraction.Ignore))
            {
                finalPos -= dir * 1.1f;
            }

            return finalPos;
        }

        public void StopFirstAction()
        { }

        public void OnNotInHand() { }
        public void OnThrow()
        {
            
        }

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
                Position = pickup.transform.position,
                Volume = _volume,
                SoundName = _stringName
            });
        }
    }
}