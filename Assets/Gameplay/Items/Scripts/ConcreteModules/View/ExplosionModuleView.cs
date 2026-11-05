using System;
using Bus;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class ExplosionModuleView : MonoBusListener
    {
        [SerializeField] private string _eventKey = "EXPLOSION_EVENT";
        
        [Header("Explosion")]
        [SerializeField] private GameObject _explosionVFX;
        
        [Header("Pshit Settings")]
        [SerializeField] private float _baseTick = 0.5f;
        [SerializeField] private float _maxTick = 0.1f;
        private float _currentPshitTick;
        private float _elapsedTick = 0;
        private bool _isActivated;
        private bool _isNormalMaterial = true;
        
        [Header("Pshit Visual")]
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private Material _pshitMaterial;
        Material _baseMaterial;

        private void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateExplosion);
            
            _currentPshitTick = _baseTick;
            _baseMaterial = _renderer.material;
        }

        private void Update()
        {
            if (!_isActivated) return;

            _elapsedTick += Time.deltaTime;

            if (_elapsedTick >= _currentPshitTick)
            {
                _elapsedTick = 0f;
                _isNormalMaterial = !_isNormalMaterial;
            }

            _renderer.sharedMaterial = _isNormalMaterial ? _baseMaterial : _pshitMaterial;
        }

        private void UpdateExplosion(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _eventKey) return;
            if (data.HostId != GetHostId()) return;

            if (data.ValueB)
            {
                _isActivated = true;
                
                UpdatePshit(data.ValueF);
            }
            else
            {
                PerformExplosion();
            }
        }
        
        private void UpdatePshit(float ratio)
        {
            if (ratio >= 0.66f)
            {
                _currentPshitTick = _maxTick;
            }
            else
            {
                _currentPshitTick = Mathf.Lerp(_baseTick, _maxTick, ratio);
            }
        }
        
        private void PerformExplosion()
        {
            GameObject vfx = Instantiate(_explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);

            if (gameObject.transform.TryGetComponent(out NetworkObject pickup))
            {
                pickup.Despawn();
            }
            
            Destroy(gameObject.transform.parent.gameObject);
        }
        
        private ulong GetHostId()
        {
            ItemPickup pickup = GetComponentInParent<ItemPickup>();
            
            if (pickup != null) 
                return pickup.NetworkObjectId;

            ItemCore core = GetComponentInParent<ItemCore>();
            return core != null ? core.HostId : ulong.MaxValue;
        }
    }
}