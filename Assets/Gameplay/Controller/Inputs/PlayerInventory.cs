using Bus;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerInventory : NetworkBusListener
    {
        [SerializeField] private PlayerInput _playerInput;

        private ItemCore _currentItem;

        void OnEnable()
        {
            
        }
    }
}