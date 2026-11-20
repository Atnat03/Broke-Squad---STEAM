using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new Action In Inventory", menuName = "Inventory/Actions/Database")]
    public class ActionInInventoryDataBase_SO : ScriptableObject
    {
        public List<ActionInInventory_SO> ActionList = new List<ActionInInventory_SO>();
    }
}