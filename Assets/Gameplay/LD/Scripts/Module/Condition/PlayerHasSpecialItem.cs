using Gameplay.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class PlayerHasSpecialItem : LDConditionModule
    {
        [Tooltip("Le joueur doit avoir un de ces items en main")]
        [SerializeField] private SO_Item[] _items;

        protected override void OnInitialize()
        {
            if (_items == null || _items.Length == 0)
                Debug.Log($"[LD] {name} : aucun items dans la liste");
        }
        public override bool CanActivate(LDActivation activation)
        {
            if (_items == null) return false;

            if (!activation.TryGetInstigator(out var player)) return false;

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return false;

            ItemInstance item = inventory.GetCurrentItemInHand();
            if (item == null) return false;

            foreach(SO_Item so in _items)
            {
                if (so == item.Data) return true;
            }

            return false;
         }
    }
}
