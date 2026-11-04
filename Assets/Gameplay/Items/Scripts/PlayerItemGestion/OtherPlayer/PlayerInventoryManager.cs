using System.Collections.Generic;
using Gameplay.Controller;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInventoryManager : MonoBehaviour
    {
        [SerializeField] private OtherInventoryUI _panelPrefab;
        [SerializeField] private Transform _container;

        private readonly Dictionary<PlayerInventory, OtherInventoryUI> _panels = new();

        private void OnEnable()
        {
            PlayerInventory.OnInventorySpawned += AddPanel;
            PlayerInventory.OnInventoryDespawned += RemovePanel;

            foreach (PlayerInventory inv in FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None))
            {
                if (inv.IsSpawned)
                    AddPanel(inv);
            }
        }

        private void OnDisable()
        {
            PlayerInventory.OnInventorySpawned -= AddPanel;
            PlayerInventory.OnInventoryDespawned -= RemovePanel;

            foreach (OtherInventoryUI panel in _panels.Values)
            {
                if (panel != null)
                    Destroy(panel.gameObject);
            }

            _panels.Clear();
        }

        private void AddPanel(PlayerInventory inventory)
        {
            if (inventory.IsOwner) return;
            if (_panels.ContainsKey(inventory)) return;

            PlayerController controller = inventory.Controller;
            
            if (controller == null)
            {
                ABPrint.Print("No controller", ABColor.Red);
                return;
            }

            OtherInventoryUI panel = Instantiate(_panelPrefab, _container);
            panel.Initialize(inventory, controller);
            _panels[inventory] = panel;
        }

        private void RemovePanel(PlayerInventory inventory)
        {
            if (_panels.Remove(inventory, out OtherInventoryUI panel) && panel != null)
                Destroy(panel.gameObject);
        }
    }
}