using Gameplay.Items;
using MyPrint;
using UnityEngine;

namespace Gameplay.LD
{
	public class KeyCondition : LDConditionModule
	{
		[Tooltip("Identifiant de la serrure.")]
		[SerializeField] private int _idKey;

		public override bool CanActivate(LDActivation activation)
		{
			if (!activation.TryGetInstigator(out var playerNO)) return false;

			PlayerInventory inventory = playerNO.GetComponentInChildren<PlayerInventory>();
			if (inventory == null) return false;

			ItemInstance item = inventory.GetCurrentItemInHand();
			if (item == null) return false;

			foreach (IFirstAction first in item.FirstActionList)
			{
				if (first is OpenDoorModule key)
				{
					bool b = KeyFits(key.Range);

					ABPrint.PrintBool(b, "B");
					
					if (b)
					{
						first.Condition.UseItem();
					}
					
					return b;
				}
			}

			return false;
		}

		private bool KeyFits(Vector2Int range)
		{
			if (range == new Vector2Int(-1, -1)) return true;
			return range.x <= _idKey && range.y >= _idKey;
		}
	}
}