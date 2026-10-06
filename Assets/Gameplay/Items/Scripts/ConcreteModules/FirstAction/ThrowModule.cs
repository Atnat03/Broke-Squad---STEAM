using System.Collections;
using System.Collections.Generic;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.FirstAction
{
    public class ThrowModule : ItemModule, ISecondAction, IServerAction
    {
        [Header("Settings")]
        [SerializeField] private Vector3 _minThrowForce = Vector3.zero;
        [SerializeField] private Vector3 _maxThrowForce = new Vector3(0f, 2f, 12f);
        [SerializeField] private float _durationToFullFill = 2;
        [SerializeField] private Vector3 _minTorqueForce = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _maxTorqueForce = new Vector3(5, 0, 0);
        [SerializeField] private Vector3 _spawnDistance = new Vector3(0,0,0.7f);
        private float _charge;

        [Header("Conditions")]        
        [SerializeReference] private List<ICondition> _conditionList = new List<ICondition>();
        
        public List<ICondition> Conditions => _conditionList;
        
        Coroutine _chargeCoroutine;
        
        public void StartSecondAction()
        {
            _chargeCoroutine = Context.StartCoroutine(FillTheThrowForce());
        }

        public void StopSecondAction()
        {
            Context.StopCoroutine(_chargeCoroutine);
            
            //Pour l'ui local
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = "UI_THROW_BAR",
                ValueF = 0,
                ValueB = false,
            });
            
            //Pour le server (le lancer)
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            { 
                ValueF = _charge,
                Rotation = Context.Camera.transform.rotation,
                Position = Context.Camera.transform.position,
            });
        }

        //Server
        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            ABPrint.Print("ServerExecute : " + data.ValueF, ABColor.Orange);
            
            Quaternion camRot = data.Rotation;
            Vector3 camPos = data.Position;
            Vector3 spawnPos = camPos + camRot * _spawnDistance;
            
            ItemPickup thrown = ServerSpawnPickup(spawnPos, camRot);
            
           if (thrown == null || !thrown.TryGetComponent(out Rigidbody rb))
                return;
            
            Vector3 force = Vector3.Lerp(_minThrowForce, _maxThrowForce, Mathf.Clamp01(data.ValueF));

            Vector3 right = camRot * Vector3.right;
            Vector3 up = camRot * Vector3.up;
            Vector3 forward = camRot * Vector3.forward;

            Transform t = Context.Camera.transform;
            
            rb.AddForce(right * force.x + up * force.y + forward * force.z, ForceMode.VelocityChange);
            
            Vector3 currentTorqueForce = Vector3.Lerp(_minTorqueForce, _maxTorqueForce, Mathf.Clamp01(data.ValueF));
            
            rb.AddTorque(t.rotation * currentTorqueForce, ForceMode.VelocityChange);
            
            foreach (IPassif passif in thrown.Instance.Passifs)
            {
                passif.OnThrow(rb);
            }
        }
        
        private ItemPickup ServerSpawnPickup(Vector3 pos, Quaternion rot)
        {
            ItemInstance item = Context.Inventory.GetCurrentItemInHand();
            if (item == null || item.Data.PickupPrefab == null)
                return null;

            GameObject newObj = Context.InstantiateGameObject(item.Data.PickupPrefab.gameObject, pos, rot);

            if (newObj == null) return null;
            if (!newObj.TryGetComponent(out ItemPickup pickup)) return null;
            
            pickup.Setup(item);
            pickup.GetComponent<NetworkObject>().Spawn();

            Context.Inventory.ClearCurrentSlot();
            return pickup;
        }
        
        private IEnumerator FillTheThrowForce()
        {
            float elapsed = 0f;
            _charge = 0f;

            while (elapsed < _durationToFullFill)
            {
                elapsed += Time.deltaTime;
                _charge = Mathf.Clamp01(elapsed / _durationToFullFill);
                
                Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
                {
                    ClientId = Context.Inventory.OwnerClientId,
                    KeyEvent = "UI_THROW_BAR",
                    ValueF = _charge,
                    ValueB = true
                });
                
                yield return null;
            }
            _charge = 1f;
        }
    }
}