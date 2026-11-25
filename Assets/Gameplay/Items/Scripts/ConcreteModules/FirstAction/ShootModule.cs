using System;
using System.Collections;
using MyPrint;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ShootModule : ActionModule, IFirstAction, IServerAction
    {
        public override string Emoji => "🔫";
        
        [SerializeField] private SO_Item _ammoPrefab;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0.2f, -0.15f, 0.7f);
        [SerializeField] private Vector3 _torqueAmmo = new Vector3(0f, 0f, 0f);
        [SerializeField] private float _wallMargin = 0.15f;
        [SerializeField] private float _shootForce;
        [SerializeField] private LayerMask _aimMask = ~0;  
        [SerializeField] private float _aimDistance = 100f;
        [SerializeField] private float _fireRate = 0.25f;
        [SerializeField] private bool _ammoAreAffectByGravity = false;
        private bool _canShoot = true;
        Coroutine _fireRateCoroutine;
        
        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;
            if (!_canShoot) return;

            _fireRateCoroutine = Context.StartCoroutine(FireRateRoutine());

            Transform cam = Context.Camera.transform;

            Vector3 aimPoint = cam.position + cam.forward * _aimDistance;
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit aim, _aimDistance, _aimMask, QueryTriggerInteraction.Ignore))
                aimPoint = aim.point;

            Vector3 target = cam.position + cam.TransformDirection(_spawnOffset);
            Vector3 toTarget = target - cam.position;
            float maxDist = toTarget.magnitude;

            Vector3 origin = target;
            if (Physics.Raycast(cam.position, toTarget.normalized, out RaycastHit hit, maxDist + _wallMargin, _aimMask, QueryTriggerInteraction.Ignore))
                origin = cam.position + toTarget.normalized * Mathf.Max(0f, hit.distance - _wallMargin);

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

        IEnumerator FireRateRoutine()
        {
            _canShoot = false;
            
            yield return new WaitForSeconds(_fireRate);

            _canShoot = true;
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

        public void OnNotInHand()
        {
            if (_fireRateCoroutine != null)
            {
                Context.StopCoroutine(_fireRateCoroutine);
                _fireRateCoroutine = null;
            }
        }
        public void OnThrow()
        {
            if (_fireRateCoroutine != null)
            {
                Context.StopCoroutine(_fireRateCoroutine);
                _fireRateCoroutine = null;
            }
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            Quaternion rotation = Quaternion.LookRotation(data.Direction);
            
            NetworkObject ammo = Context.SpawnGameObject(_ammoPrefab.PickupPrefab.gameObject, data.Position, rotation);
            
            ItemInstance item = new ItemInstance(_ammoPrefab);
            
            if (item.Data.PickupPrefab == null)
                return;
            
            if (ammo.TryGetComponent(out ItemPickup pickup))
            {
                pickup.Setup(item);
                
                pickup.SkipCollisionBuffer();

                Collider[] shooterCols = Context.Inventory.GetComponentsInChildren<Collider>();
                foreach (Collider a in ammo.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider s in shooterCols)
                        Physics.IgnoreCollision(a, s);
                }
            }
            
            if (ammo.TryGetComponent(out Rigidbody rb))
            {
                foreach (IPassif p in item.Passifs)
                {
                    p.OnThrow(rb);
                }
                
                float force = data.ValueF * rb.mass;
                
                rb.useGravity = _ammoAreAffectByGravity;
                pickup.OnCollide += () => rb.useGravity = true;
                
                rb.AddTorque(_torqueAmmo);
                rb.AddForce(data.Direction * force, ForceMode.Impulse);
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