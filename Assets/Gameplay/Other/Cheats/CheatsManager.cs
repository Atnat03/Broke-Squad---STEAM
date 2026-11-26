using System;
using System.Collections.Generic;
using Bus;
using Gameplay.IA;
using Gameplay.Items;
using Gameplay.Other.Cheat;
using Gameplay.PlayerData;
using Network.Connections.SceneLoading;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Gameplay.Controller
{
    public class CheatsManager : NetworkBusListener
    {
        [Header("References")] [SerializeField]
        private PlayerHealth _playerHealth;

        [SerializeField] private PlayerRevive _playerRevive;
        [SerializeField] private PlayerController _playerController;

        private Player _playerInputs;

        [Header("Item Give")] [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private CheatGiveItemElement _prefabCheatItemGive;
        [SerializeField] private Transform _parentGiveItemSpawn;
        [SerializeField] private GameObject _itemGiveUI;
        [SerializeField] private SO_ItemDataBase _database;
        private bool _isOpen = false;

        [Header("Lock Down")] [SerializeField] private Button _stopAggroButton;

        [Header("Teleport")] [SerializeField] private List<Button> _playerTeleportsButton;
        [SerializeField] private Button _spawnTeleportButton;

        [Header("Noclip")] [SerializeField] private float _noclipSpeed = 8f;
        [SerializeField] private float _noclipSprintMultiplier = 3f;
        [SerializeField] private TextMeshProUGUI _noclipText;

        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private Camera _noclipCam;
        private bool _noclip;

        public Action OnInvincibility;
        public Action OnRevive;
        public Action OnDamage;
        public Action OnHeal;

        void Awake()
        {
            _itemGiveUI.SetActive(false);
            _noclipText.gameObject.SetActive(false);
            foreach (SO_Item item in _database.Data)
            {
                CheatGiveItemElement elem = Instantiate(_prefabCheatItemGive, _parentGiveItemSpawn);
                elem.Initialise(item, () => TryGiveItem(item.ID));
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
                return;

            _spawnTeleportButton.onClick.AddListener(() =>
            {
                _playerController.TeleportClientRpc(SpawnPointManager.instance.GetPlayerSpawnPoint(0).position,
                    SpawnPointManager.instance.GetPlayerSpawnPoint(0).transform.rotation);
            });

            _playerInputs = new Player();

            _playerInputs.Enable();
            _playerInputs.Cheats.Invicibility.performed += Invicibility;
            _playerInputs.Cheats.Heal.performed += Heal;
            _playerInputs.Cheats.Revive.performed += Revive;
            _playerInputs.Cheats.TakeDamage.performed += Damage;
            _playerInputs.Gameplay.Pause.performed += LeavePanel;
            _playerInputs.Cheats.NoClip.performed += _ => ToggleNoclip();

            _playerInputs.Cheats.CheatPannel.performed += UpdatePannelItemGive;


            _rb = _playerController.GetComponent<Rigidbody>();
            _capsule = _playerController.GetComponent<CapsuleCollider>();
            _noclipText.gameObject.SetActive(false);


            if (!IsServer)
                _stopAggroButton.gameObject.SetActive(false);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _playerInputs.Cheats.Invicibility.performed -= Invicibility;
                _playerInputs.Cheats.Heal.performed -= Heal;
                _playerInputs.Cheats.Revive.performed -= Revive;
                _playerInputs.Cheats.TakeDamage.performed -= Damage;
                _playerInputs.Gameplay.Pause.performed -= LeavePanel;
                _playerInputs.Cheats.CheatPannel.performed -= UpdatePannelItemGive;
                _playerInputs.Cheats.NoClip.performed += _ => ToggleNoclip();
                _playerInputs.Disable();

                if (_noclip) SetNoclip(false);
            }

            base.OnNetworkDespawn();
        }

        public void UnlockController()
        {
            for (int i = 0; i < 3; i++)
            {
                //Loop just in case they are many lock active
                _playerController.PopLock();
            }

        }

        public void ToggleInfiniteStamina()
        {
            _playerController.Stamina.ToggleInfiniteStamina();
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
            _playerController.SetInputEnabled(_isOpen);
            _isOpen = !_isOpen;
            _itemGiveUI.SetActive(_isOpen);

            if (_isOpen) RequestPlayersServerRpc();

            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isOpen;
        }


        private void TryGiveItem(int id)
        {
            if (!IsServer)
            {
                TryGiveItemServerRpc(id);
                return;
            }

            _inventory.TryAddItem(new ItemInstance(_database.GetItemData(id)));
        }

        [Rpc(SendTo.Server)]
        private void TryGiveItemServerRpc(int id) => TryGiveItem(id);

    #endregion

        private void LeavePanel(InputAction.CallbackContext context)
        {
            if (_isOpen) UpdatePannelItemGive(context);
        }
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

        public void DesactivateAllGuardAggro()
        {
            if (!IsServer) return;

            LockDownManager lockDownManager = LockDownManager.instance;
            if (lockDownManager)
            {
                lockDownManager.StopAllGuardsAggro();
            }
            
            DesactivateAllGuardAggroClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void DesactivateAllGuardAggroClientRpc()
        {
            InvokeEvent(new OnLockDownEnd_EVENT());
        }
        
        #region Noclip

        public void ToggleNoclip() => SetNoclip(!_noclip);
        
        private void SetNoclip(bool value)
        {
            if (_noclip == value) return;
            _noclipText.gameObject.SetActive(value);
            _playerController.PlayerAnimator.SetAnimBool(value, "NoClip");
            if (value)
            {
                _noclip = true;
                _playerController.NoclipActive = true;
                _capsule.isTrigger = true;          
                _playerController.Motor.StopMoving();
            }
            else
            {
                _capsule.isTrigger = false;
                _playerController.NoclipActive = false;
                _noclip = false;
                _playerController.Motor.StopMoving();
            }
        }
        
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner || !_noclip) return;

            if (_noclipCam == null) _noclipCam = _playerController.PlayerCamera.GetComponent<Camera>();
            Transform view = _noclipCam != null ? _noclipCam.transform : _playerController.transform;

            Vector2 move = _playerController.MoveInput;
            Vector3 dir = view.forward * move.y + view.right * move.x;
            
            bool canAct = _playerController.InputEnabled;
            if (canAct && _playerInputs.Gameplay.Jump.IsPressed()) dir += Vector3.up;
            if (canAct && _playerController.CrouchHeld) dir += Vector3.down;

            dir = Vector3.ClampMagnitude(dir, 1f);

            float speed = _noclipSpeed * (_playerController.SprintHeld ? _noclipSprintMultiplier : 1f);
            _rb.linearVelocity = dir * speed;
        }
        
        #endregion
    }
}