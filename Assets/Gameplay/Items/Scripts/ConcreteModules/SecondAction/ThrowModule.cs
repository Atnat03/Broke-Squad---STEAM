using System;
using System.Collections;
using System.Collections.Generic;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ThrowModule : ActionModule, ISecondAction, IServerAction
    {
        public override string Emoji => "🤾";
        
        [Header("Settings")]
        [SerializeField] private float _durationToFullFill = 0.5f;
        
        [Header("Throw Force")]
        [SerializeField] private Vector3 _minThrowForce = Vector3.zero;
        [SerializeField] private Vector3 _maxThrowForce = new Vector3(0f, 2f, 12f);
        
        [Header("Throw Torque")]
        [SerializeField] private Vector3 _minTorqueForce = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _maxTorqueForce = new Vector3(5, 0, 0);
        
        [Header("Spawn Position")]
        [SerializeField] private Vector3 _spawnDistance = new Vector3(0.2f,0,0.7f);
        
        private float _charge;
        private Coroutine _chargeCoroutine;

        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;

        private bool _isInputPressed = false;
        
        private readonly List<MeshRenderer> _hiddenMeshes = new();
        
        #region Action Gestion

        public ICondition Condition => ConditionParent;
        
        public void StartSecondAction()
        {
            if (!CanUse()) return;
            
            _isInputPressed = true;
            
            _chargeCoroutine = Context.StartCoroutine(FillTheThrowForce());
        }

        public void StopSecondAction()
        {
            if (_chargeCoroutine == null) return;
            
            if (!_isInputPressed)
                return;

            Context.StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = null;

            SendToUI(false);

            ItemInstance held = Context.Inventory.GetHeldInstance();
            if (held == null || held.Data == null || held.Data.PickupPrefab == null) return;

            Vector3 pos = Context.Camera.transform.position;
            Vector3 forward = Context.Camera.transform.forward;
            Quaternion camRot = Context.Camera.transform.rotation;

            Vector3 finalPos = GetThrowStartPos(pos, forward);
            
            if (!Context.Inventory.IsServer)
            {
                ComputeThrow(_charge, camRot, finalPos, out var spawnPos, out var lin, out var ang);
                
                ThrowGhost.Create(held.Data.PickupPrefab, spawnPos,
                    camRot * held.Data.PickupPrefab.transform.rotation,
                    lin, ang,
                    Context.Inventory.GetComponentsInChildren<Collider>(),
                    onUnclaimed: () => SetHandVisible(true));
                
                SetHandVisible(false);
            }
            
            foreach (IFirstAction firstAction in held.FirstActionList)
            {
                firstAction.OnThrow();
            }
            
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                ValueF = _charge,
                Rotation = camRot,
                Position = finalPos,
            });
            
            _isInputPressed = false;
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        // Calculate if a wall is a front of us
        private Vector3 GetThrowStartPos(Vector3 pos, Vector3 dir)
        {
            Vector3 finalPos = pos;
            
            if (Physics.Raycast(pos, dir, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                finalPos -= dir * 1.1f;
            }
            
            return finalPos;
        }

        #endregion
        
        //Server
        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            ComputeThrow(data.ValueF, data.Rotation, data.Position, out Vector3 spawnPos, out Vector3 linear, out Vector3 angular);

            ItemPickup thrown = ServerSpawnPickup(spawnPos, data.Rotation);
            if (thrown == null || !thrown.TryGetComponent(out Rigidbody rb))
                return;
            
            rb.AddForce(linear, ForceMode.VelocityChange);
            rb.AddTorque(angular, ForceMode.VelocityChange);
            
            //SFX
            Context.Inventory.InvokeEvent(new PlaySoundEvent
            {
                Position = thrown.transform.position,
                Volume = _volume,
                SoundName = _stringName
            });
            
            foreach (IPassif passif in thrown.Instance.Passifs)
            {
                passif.OnThrow(rb);
            }
        }

        private void SetHandVisible(bool visible)
        {
            if (!visible)
            {
                if (Context?.Core == null) return;
                foreach (var m in Context.Core.GetComponentsInChildren<MeshRenderer>())
                    if (m.enabled) { m.enabled = false; _hiddenMeshes.Add(m); }
                return;
            }

            foreach (var m in _hiddenMeshes) if (m) m.enabled = true;
            _hiddenMeshes.Clear();
        }
        
        private void ComputeThrow(float charge, Quaternion camRot, Vector3 camPos, out Vector3 spawnPos, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            float t = Mathf.Clamp01(charge);

            spawnPos = camPos + camRot * _spawnDistance;
            linearVelocity = camRot * Vector3.Lerp(_minThrowForce, _maxThrowForce, t);
            angularVelocity = camRot * Vector3.Lerp(_minTorqueForce, _maxTorqueForce, t);
        }

        public void OnNotInHand()
        {
            Context.StopCoroutine(_chargeCoroutine);

            _isInputPressed = false;
            
            SendToUI(false);
        }

        // Faire spawn le nouvel item via la fonction de Context
        private ItemPickup ServerSpawnPickup(Vector3 pos, Quaternion rot)
        {
            ItemInstance item = Context.Inventory.GetCurrentItemInHand();
            if (item == null || item.Data.PickupPrefab == null)
                return null;

            GameObject newObj = Context.InstantiateGameObject(
                item.Data.PickupPrefab.gameObject, 
                pos, 
                rot * item.Data.PickupPrefab.transform.rotation);

            if (newObj == null) return null;
            if (!newObj.TryGetComponent(out ItemPickup pickup)) return null;
            
            pickup.ThrownBy.Value = Context.Inventory.OwnerClientId;
            pickup.GetComponent<NetworkObject>().Spawn();
            pickup.Setup(item);

            Context.Inventory.ClearCurrentSlot();
            return pickup;
        }
        
        //Coroutine de load du lancer
        private IEnumerator FillTheThrowForce()
        {
            float elapsed = 0f;
            _charge = 0f;

            while (elapsed < _durationToFullFill)
            {
                elapsed += Time.deltaTime;
                _charge = Mathf.Clamp01(elapsed / _durationToFullFill);
                
                // Envoie un event à destination de l'ui avec la key : UI_THROW_BAR

                SendToUI(true);
                
                yield return null;
            }
            _charge = 1f;
        }

        private void SendToUI(bool state)
        {
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = "UI_THROW_BAR",
                ValueF = _charge,
                ValueB = state
            });
        }
    }
}