using System;
using System.Collections;
using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ThrowModule : ItemModule, IRightClick
    {
        [SerializeField] private Vector3 _minThrowForce = Vector3.zero;
        [SerializeField] private Vector3 _maxThrowForce = new Vector3(0f, 2f, 12f);
        [SerializeField] private float _durationToFullFill = 2;
        [SerializeField] private Vector3 _torqueForce = new Vector3(5, 0, 0);
        [SerializeField] private float _spawnDistance = 0.7f;

        private float _charge;

        public float SpawnDistance => _spawnDistance;

        public void StartRightClick()
        {
            Context.Inventory.EnableBar(true);
            Context.Core.StartModuleCoroutine(FillTheThrowForce());
        }

        private IEnumerator FillTheThrowForce()
        {
            float elapsed = 0f;
            _charge = 0f;

            while (elapsed < _durationToFullFill)
            {
                elapsed += Time.deltaTime;
                _charge = Mathf.Clamp01(elapsed / _durationToFullFill);
                Context.Inventory.UpdateBar(_charge);
                yield return null;
            }
            _charge = 1f;
        }

        public void EndRightClick()
        {
            PlayerInventory inv = Context.Inventory;
            inv.EnableBar(false);

            if (!inv.HasItemInHand())
                return;

            Transform cam = inv.PlayerCamera.transform;
            inv.RequestThrow(_charge, cam.position, cam.rotation);
            _charge = 0f;
        }

        public void ApplyThrow(ItemPickup thrown, float charge01, Quaternion camRot)
        {
            if (thrown == null || !thrown.TryGetComponent(out Rigidbody rb))
                return;

            Vector3 force = Vector3.Lerp(_minThrowForce, _maxThrowForce, Mathf.Clamp01(charge01));

            Vector3 right = camRot * Vector3.right;
            Vector3 up = camRot * Vector3.up;
            Vector3 forward = camRot * Vector3.forward;

            Transform t = Context.Camera.transform;
            
            rb.AddForce(right * force.x + up * force.y + forward * force.z, ForceMode.VelocityChange);
            rb.AddTorque(t.rotation * _torqueForce, ForceMode.VelocityChange);

            foreach (IPassif passif in thrown.Instance.Passifs)
            {
                passif.OnThrow(rb);
            }
        }
    }
}