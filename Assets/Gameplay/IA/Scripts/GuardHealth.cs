using Bus;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardHealth : NetworkBusListener, IDamageable
    {
        public float Health => _currentHealth.Value;
        
        [SerializeField] private float _baseHealth = 100;
        
        private readonly NetworkVariable<float> _currentHealth = new NetworkVariable<float>(0);
        private readonly NetworkVariable<bool> _isKO = new NetworkVariable<bool>();

        public void ApplyDamage(float damage)
        {
            if(!IsServer)
            {
                ApplyDamageServerRpc(damage);
                return;
            }
            
            _currentHealth.Value -= damage;

            if (_currentHealth.Value <= 0)
            {
                _isKO.Value = true;
            }
        }
        
        [ServerRpc]
        private void ApplyDamageServerRpc(float damage) => ApplyDamage(damage);
    }
}