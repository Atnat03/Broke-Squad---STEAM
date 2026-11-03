using System;
using Bus;
using Gameplay.Items;
using Gameplay.Other.Cheat;
using Gameplay.PlayerData;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Gameplay.Controller
{
    public class CheatsManager : NetworkBusListener
    {
        [Header("References")] 
        public PlayerHealth  playerHealth; 
        public PlayerRevive playerRevive;
        
        private Player _playerInputs;
            
        [Header("Item Give")]
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private CheatGiveItemElement _prefabCheatItemGive;
        [SerializeField] private Transform _parentGiveItemSpawn;
        [SerializeField] private GameObject _itemGiveUI;
        [SerializeField] private SO_ItemDataBase _database;
        private bool _isOpen = false;
        
        public Action OnInvincibility;
        public Action OnRevive;
        public Action OnDamage;
        public Action OnHeal;
        
        void Awake()
        {
            _playerInputs = new Player();

            _itemGiveUI.SetActive(false);
            
            ABPrint.Print("Create new Item : " +_database.Data.Count);
            
            foreach (SO_Item item in _database.Data)
            {
                ABPrint.Print("Create new Item");
                CheatGiveItemElement elem = Instantiate(_prefabCheatItemGive, _parentGiveItemSpawn);
                elem.Initialise(item, () => _inventory.TryAddItem(new ItemInstance(item)));
            }
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
            
            _playerInputs.Cheats.CheatPannel.performed += UpdatePannelItemGive;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _playerInputs.Cheats.Invicibility.performed -= Invicibility;
                _playerInputs.Cheats.Heal.performed -= Heal;
                _playerInputs.Cheats.Revive.performed -= Revive;
                _playerInputs.Cheats.TakeDamage.performed -= Damage;
                
                _playerInputs.Cheats.CheatPannel.performed -= UpdatePannelItemGive;
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
        
        #region Give Item

        private void UpdatePannelItemGive(InputAction.CallbackContext context)
        {
            _isOpen = !_isOpen;
            
            _itemGiveUI.SetActive(_isOpen);
            
            Cursor.visible = !_isOpen;
            Cursor.lockState = !_isOpen ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = _isOpen;
        }
        
        #endregion
        
    }
}