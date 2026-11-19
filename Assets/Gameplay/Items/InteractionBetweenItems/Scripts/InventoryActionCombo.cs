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
        ActionInInventory_SO _currentAIV;
        
        public override void OnNetworkSpawn()
        {
            _playerInventory.OnPickupItem += CheckIfCanCombo;

            _playerInput.OnActionInInventory += DoActionInInventory;
        }

        private void DoActionInInventory()
        {
            if (!IsServer)
            {
                DoAIVServerRpc();
                return;
            }
            
            
            if (_playerInventory.RealNumberItemInInventory() < 2) return;
            if (_currentAIV != null)
            {
                ItemInstance itemA = _playerInventory.ItemsInInventory[0];
                ItemInstance itemB = _playerInventory.ItemsInInventory[1];
                _currentAIV.DoTheAction(itemA, itemB);
            }
        }
        
        [Rpc(SendTo.Server)]
        private void DoAIVServerRpc() => DoActionInInventory();

        private void CheckIfCanCombo()
        {
            if (_playerInventory.RealNumberItemInInventory() < 2) return;
            
            foreach (ActionInInventory_SO action in _dataBaseAction.ActionList)
            {
                if (action.HasAllOfTheItem(_playerInventory.ItemsInInventory))
                {
                    _currentAIV = action;
                    return;
                }
            }
        }
    }
}