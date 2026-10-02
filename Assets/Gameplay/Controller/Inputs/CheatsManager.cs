using System;
using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Controller
{
    public class CheatsManager : NetworkBusListener
    {
        private Player _playerInputs;
        public PlayerData.PlayerData  playerData; 
        public PlayerResurrection playerResurrection;
        
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
            playerData.invincibility.Value = !playerData.invincibility.Value;
        }

        private void Damage(InputAction.CallbackContext context)
        {
            DamageServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void DamageServerRpc()
        {
            playerData.ApplyDamage(50);
        }

        private void Heal(InputAction.CallbackContext context)
        {
            HealServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void HealServerRpc()
        {
            playerData.Heal(playerData.MaxHp);
        }

        private void Revive(InputAction.CallbackContext context)
        {
            playerResurrection.ReviveOwnerRpc();
        }
        
    }
}