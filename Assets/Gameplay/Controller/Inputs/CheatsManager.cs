using System;
using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Gameplay.Controller
{
    public class CheatsManager : NetworkBusListener
    {
        private Player _playerInputs;
        public PlayerHealth  playerHealth; 
        public PlayerRevive playerRevive;
            
        public Action OnInvincibility;
        public Action OnRevive;
        public Action OnDamage;
        public Action OnHeal;
        
        
        void Awake()
        {
            _playerInputs = new Player();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner) return;

            _playerInputs.Enable();
            _playerInputs.Cheats.Invicibility.performed += Invicibility;
            _playerInputs.Cheats.Heal.performed += Heal;
            _playerInputs.Cheats.Revive.performed += Revive;
            _playerInputs.Cheats.TakeDamage.performed += Damage;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _playerInputs.Cheats.Invicibility.performed -= Invicibility;
                _playerInputs.Cheats.Heal.performed -= Heal;
                _playerInputs.Cheats.Revive.performed -= Revive;
                _playerInputs.Cheats.TakeDamage.performed -= Damage;
                _playerInputs.Disable();
            }
            base.OnNetworkDespawn();
        }

        private void Invicibility(InputAction.CallbackContext context)
        {
            InvincibilityServerRpc();
        }
        
        [Rpc(SendTo.Server)]
        private void InvincibilityServerRpc()
        {
            playerHealth.SetInvincible(!playerHealth.IsInvincible);
        }

        private void Damage(InputAction.CallbackContext context)
        {
            DamageServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void DamageServerRpc()
        {
            playerHealth.ApplyDamage(50);
        }

        private void Heal(InputAction.CallbackContext context)
        {
            HealServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void HealServerRpc()
        {
            playerHealth.Heal(playerHealth.MaxHp);
        }

        private void Revive(InputAction.CallbackContext context) => ReviveServerRpc();

        [Rpc(SendTo.Server)]
        private void ReviveServerRpc() => playerHealth.ServerRevive();
        
    }
}