using System;
using System.Collections;
using System.Numerics;
using Gameplay.Items.Scripts.PlayerItemGestion;
using MyPrint;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ThrowModule : ItemModule, IRightClick
    {
        [SerializeField] private Vector3 _minThrowForce = Vector3.zero;
        [SerializeField] private Vector3 _maxThrowForce = new Vector3(0f, 2f, 12f);
        [SerializeField] private float _durationToFullFill = 2;
        [SerializeField] private float _torqueForce = 5f;
        [SerializeField] private float _spawnDistance = 0.7f;

        private float _elapsedFillingTime = 0;
        private Vector3 _throwForce;
        
        public void StartRightClick()
        {
            Context.Inventory.EnableBar(true);
            
            Context.Core.StartModuleCoroutine(FillTheThrowForce());
        }

        IEnumerator FillTheThrowForce()
        {
            _elapsedFillingTime = 0;
            
            while (_elapsedFillingTime < _durationToFullFill)
            {
                _elapsedFillingTime += Time.deltaTime;

                float t = _elapsedFillingTime / _durationToFullFill;
                
                Context.Inventory.UpdateBar(t);
                
                _throwForce = Vector3.Lerp(_minThrowForce, _maxThrowForce, t);
                
                yield return null;
            }
        }

        public void EndRightClick()
        {
            Context.Inventory.EnableBar(false);
            
            PlayerInventory inv = Context.Inventory;
            
            if (!inv.HasItemInHand()) 
                return;

            Transform cam = inv.PlayerCamera.transform;

            Vector3 spawnPos = cam.position + cam.forward * _spawnDistance;
            ItemPickup thrown = inv.DropItem(spawnPos, Context.Core.transform.rotation);

            inv.DestroyItemInHand();

            if (thrown == null) return;

            if (thrown.TryGetComponent(out Rigidbody rb))
            {
                Vector3 velocity =
                    cam.right * _throwForce.x +
                    cam.up * _throwForce.y +
                    cam.forward * _throwForce.z;

                rb.AddForce(velocity, ForceMode.VelocityChange);
                rb.AddTorque(cam.right * _torqueForce, ForceMode.VelocityChange);
            }
        }
    }
}