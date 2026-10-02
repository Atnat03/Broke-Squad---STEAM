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
        
        public Action OnInvincibility;
        public Action OnRevive;
        public Action OnDamage;
        public Action OnHeal;
        
        
        void Awake()
        {
            _playerInputs = new Player();
        }

        void OnEnable()
        {
            _playerInputs.Enable();

            _playerInputs.Cheats.Invicibility.performed += InvincibilityServerRpc;
            _playerInputs.Cheats.Heal.performed += HealServerRpc;
            _playerInputs.Cheats.Revive.performed += ReviveServerRpc;
            _playerInputs.Cheats.TakeDamage.performed += DamageServerRpc;
            
        }
        
        
        [Rpc(SendTo.Server)]
        private void InvincibilityServerRpc(InputAction.CallbackContext context)
        {
            playerData.invincibility.Value = !playerData.invincibility.Value;
        }

        [Rpc(SendTo.Server)]
        private void DamageServerRpc(InputAction.CallbackContext context)
        {
            playerData.ApplyDamage(50);
        }

        [Rpc(SendTo.Server)]
        private void HealServerRpc(InputAction.CallbackContext context)
        {
            playerData.Heal(playerData.MaxHp);
        }

        [Rpc(SendTo.Server)]
        private void ReviveServerRpc(InputAction.CallbackContext context)
        {
            InvokeEvent(new PlayerRevivedEvent { playerID = OwnerClientId });
        }
    }
}