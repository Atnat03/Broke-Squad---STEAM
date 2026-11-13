using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Items;
using Gameplay.Other.Cheat;
using Gameplay.PlayerData;
using MyPrint;
using Network.Connections.SceneLoading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.Controller
{
    public class CheatsManager : NetworkBusListener
    {
        [Header("References")] 
        [SerializeField] private PlayerSetUpBridge _bridge;
        private PlayerHealth  _playerHealth; 
        private PlayerRevive _playerRevive;
        private PlayerController _playerController;
        
        private Player _playerInputs;
            
        [Header("Item Give")]
        private PlayerInventory _inventory;
        [SerializeField] private CheatGiveItemElement _prefabCheatItemGive;
        [SerializeField] private Transform _parentGiveItemSpawn;
        [SerializeField] private GameObject _itemGiveUI;
        [SerializeField] private SO_ItemDataBase _database;
        private bool _isOpen = false;
        
        [Header("Teleport")]
        [SerializeField] private List<Button> _playerTeleportsButton;
        [SerializeField] private Button _spawnTeleportButton;
        
        public Action OnInvincibility;
        public Action OnRevive;
        public Action OnDamage;
        public Action OnHeal;
        
        void Awake()
        {
            _playerInputs = new Player();
            
            _itemGiveUI.SetActive(false);
            
            foreach (SO_Item item in _database.Data)
            {
                CheatGiveItemElement elem = Instantiate(_prefabCheatItemGive, _parentGiveItemSpawn);
                elem.Initialise(item, () => _inventory.TryAddItem(new ItemInstance(item)));
            }
        }

        private void Start()
        {
            _playerController = _bridge.PlayerController;
            _playerHealth = _bridge.PlayerHealth;
            _playerRevive = _bridge.PlayerHealth.PlayerRevive;
            _inventory = _bridge.PlayerInventory;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner) return;

            _spawnTeleportButton.onClick.AddListener(() =>
            {
                _playerController.TeleportClientRpc(SpawnPointManager.instance.GetPlayerSpawnPoint(0).position, SpawnPointManager.instance.GetPlayerSpawnPoint(0).transform.rotation);
            });
            
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
            _playerHealth.SetInvincible(!_playerHealth.IsInvincible);
        }

        private void Damage(InputAction.CallbackContext context)
        {
            DamageServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void DamageServerRpc()
        {
            _playerHealth.ApplyDamage(50);
        }

        private void Heal(InputAction.CallbackContext context)
        {
            HealServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void HealServerRpc()
        {
            _playerHealth.Heal(_playerHealth.MaxHp);
        }

        private void Revive(InputAction.CallbackContext context) => ReviveServerRpc();

        [Rpc(SendTo.Server)]
        private void ReviveServerRpc() => _playerHealth.ServerRevive();
        
        #region Give Item

        private void UpdatePannelItemGive(InputAction.CallbackContext context)
        {
            _isOpen = !_isOpen;
            _itemGiveUI.SetActive(_isOpen);

            if (_isOpen) RequestPlayersServerRpc();

            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isOpen;
        }
        
        #endregion
        
        [Rpc(SendTo.Server)]
        private void RequestPlayersServerRpc()
        {
            var ids = new List<ulong>();
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (id == OwnerClientId) continue; 
                ids.Add(id);
            }
            SendPlayersOwnerRpc(ids.ToArray());
        }
        
        [Rpc(SendTo.Owner)]
        private void SendPlayersOwnerRpc(ulong[] ids)
        {
            for (int i = 0; i < _playerTeleportsButton.Count; i++)
            {
                Button btn = _playerTeleportsButton[i];
                btn.onClick.RemoveAllListeners();

                bool present = i < ids.Length;
                btn.gameObject.SetActive(present);
                if (!present) continue;

                ulong targetId = ids[i];

                var label = btn.GetComponentInChildren<TMPro.TMP_Text>();
                if (label) label.text = $"Player {targetId + 1}";

                btn.onClick.AddListener(() => TeleportToPlayerServerRpc(targetId));
            }
        }
        
        [Rpc(SendTo.Server)]
        private void TeleportToPlayerServerRpc(ulong targetId)
        {
            var target = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetId);
            if (target == null) return;

            _playerController.TeleportClientRpc(target.transform.position, target.transform.rotation);
        }
    }
}