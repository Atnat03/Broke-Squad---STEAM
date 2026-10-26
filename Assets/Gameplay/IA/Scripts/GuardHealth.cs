using System;
using System.Collections;
using Bus;
using Gameplay.IA.Scripts;
using Gameplay.LD.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardHealth : NetworkBusListener, IDamageable, IStunnable
    {
        public float HealthRatio => _currentHealth.Value / _baseHealth;
        public bool IsKO => _isKO.Value;
        
        [SerializeField] private float _baseHealth = 100;
        
        private readonly NetworkVariable<float> _currentHealth = new NetworkVariable<float>(0);
        private readonly NetworkVariable<bool> _isKO = new NetworkVariable<bool>();

        public GuardBehavior GuardBrain;
        
        //Action
        public Action OnTakeDamage;
        public Action<bool> OnStun;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            
            _currentHealth.Value = _baseHealth;
        }

        public void ApplyDamage(float damage)
        {
            if(!IsServer)
            {
                ApplyDamageServerRpc(damage);
                return;
            }

            if (IsKO) return;
            
            _currentHealth.Value -= damage;
            
            OnTakeDamage?.Invoke();

            GuardBrain.ResetTimerAlert();
            
            if (_currentHealth.Value <= 0)
            {
                _isKO.Value = true;
            }
        }
        
        [ServerRpc]
        private void ApplyDamageServerRpc(float damage) => ApplyDamage(damage);

        public void ApplyStun(float stunDuration)
        {
            if (!IsServer)
            {
                ApplyStunRpc(stunDuration);
                return;
            }
            
            PerformStunClientRpc(stunDuration);
            
        }
        
        [Rpc(SendTo.Server)]
        private void ApplyStunRpc(float stunDuration) => ApplyStun(stunDuration);
        
        [Rpc(SendTo.Everyone)]
        private void PerformStunClientRpc(float stunDuration)
        {
            StartCoroutine(PerformStun(stunDuration));
        }
        
        IEnumerator PerformStun(float stunDuration)
        {
            OnStun?.Invoke(true);
            yield return new WaitForSeconds(stunDuration);
            OnStun?.Invoke(false);
        }
    }
}