using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new Action In Inventory", menuName = "Inventory/Actions/Action In Inventory")]
    public class ActionInInventory_SO : ScriptableObject
    {
        [Header("Items needed")]
        [SerializeField] public SO_Item ItemToDoFirstAction;
        [SerializeField] public SO_Item ItemToConsumeCondition;
        [SerializeReference] public IActionInInventory ActionInInventory;

        public void DoTheAction(ItemInstance itemA, ItemInstance itemB)
        {
            ActionInInventory?.DoAction(itemA, itemB);
        }
        
        public bool HasAllOfTheItem(ItemInstance[] items)
        {
            foreach (var s in items)
            {
                if (s.Data.ID != ItemToDoFirstAction.ID && s.Data.ID != ItemToConsumeCondition.ID)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}