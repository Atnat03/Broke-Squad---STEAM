using System;
using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class InventoryActionCombo : NetworkBusListener
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private ActionInInventoryDataBase_SO _dataBaseAction;

        private ActionInInventory_SO _currentAIV;

        public override void OnNetworkSpawn()
        {
            _playerInventory.OnSlotsChanged += CheckIfCanCombo;
            _playerInput.OnActionInInventory += DoActionInInventory;
        }

        public override void OnNetworkDespawn()
        {
            _playerInventory.OnSlotsChanged -= CheckIfCanCombo;
            _playerInput.OnActionInInventory -= DoActionInInventory;

            base.OnNetworkDespawn();
        }

        private void CheckIfCanCombo()
        {
            if (!IsOwner)
                return;

            CheckComboServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void CheckComboServerRpc()
        {
            if (_playerInventory.RealNumberItemInInventory() < 2)
            {
                SendComboResultToOwnerRpc(-1);
                return;
            }

            ActionInInventory_SO actionFound = null;

            foreach (ActionInInventory_SO action in _dataBaseAction.ActionList)
            {
                if (action.HasAllOfTheItem(_playerInventory.ItemsInInventory))
                {
                    actionFound = action;
                    break;
                }
            }

            int actionIndex = actionFound ? _dataBaseAction.ActionList.IndexOf(actionFound) : -1;

            SendComboResultToOwnerRpc(actionIndex);
        }

        [Rpc(SendTo.Owner)]
        private void SendComboResultToOwnerRpc(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= _dataBaseAction.ActionList.Count)
            {
                _currentAIV = null;
                _playerInventory.OnCanComboUpdate?.Invoke(false, null);
                return;
            }

            _currentAIV = _dataBaseAction.ActionList[actionIndex];

            _playerInventory.OnCanComboUpdate?.Invoke(true, _currentAIV);
        }

        private void DoActionInInventory()
        {
            if (!IsOwner)
                return;

            DoActionServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void DoActionServerRpc()
        {
            if (_playerInventory.RealNumberItemInInventory() < 2)
                return;

            ActionInInventory_SO action = null;

            foreach (ActionInInventory_SO a in _dataBaseAction.ActionList)
            {
                if (a.HasAllOfTheItem(_playerInventory.ItemsInInventory))
                {
                    action = a;
                    break;
                }
            }

            if (action == null)
                return;

            ItemInstance itemA = _playerInventory.ItemsInInventory[0];
            ItemInstance itemB = _playerInventory.ItemsInInventory[1];

            action.DoTheAction(itemA, itemB);
        }
    }
}