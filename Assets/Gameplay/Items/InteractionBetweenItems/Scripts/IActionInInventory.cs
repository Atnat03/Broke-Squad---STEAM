namespace Gameplay.Items
{
    public interface IActionInInventory
    {
        public void DoAction(ItemInstance myself, ItemInstance otherItem);
    }
}