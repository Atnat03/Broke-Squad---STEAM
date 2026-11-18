using Gameplay.Items;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	public enum TriggerType
	{
		Player,
		Object,
		Both
	}

	[RequireComponent(typeof(Collider))]
	public class ObjectStayTrigger : LDTriggerModule
	{
		[Tooltip("Type de trigger : Player, Object ou les deux")]
		[SerializeField] private TriggerType _triggerType = TriggerType.Player;

		[Tooltip("Poid du joueur")]
		[SerializeField] private float _playerWeightValue = 10f;

		[Tooltip("Poids minimum total des objets pour déclencher le trigger")]
		[SerializeField, Min(0f)] private float _weightToBeActivated = 1f;

		[Tooltip("Le flag mis à jour : actif tant que la plaque est pressée.")]
		[SerializeField] private LDFlag _flag = LDFlag.Activated;

		private readonly HashSet<Collider> _colliders = new();
		private readonly HashSet<NetworkObject> _occupants = new();

		protected override void OnInitialize()
		{
			if (TryGetComponent<Collider>(out var collider))
				collider.isTrigger = true;
			if (!TryGetComponent<Rigidbody>(out _))
				gameObject.AddComponent<Rigidbody>().isKinematic = true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (Context == null || !Context.IsServer) return;
			NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
			if (netObj == null || netObj == Context.Element.NetworkObject) return;
			if (!Accepts(netObj)) return;
			_colliders.Add(other);
		}

		private void OnTriggerExit(Collider other)
		{
			if (Context == null || !Context.IsServer) return;
			_colliders.Remove(other);
		}
		private bool Accepts(NetworkObject netObj)
		{
			bool isPlayer = netObj.IsPlayerObject;
			return _triggerType switch
			{
				TriggerType.Player => isPlayer,
				TriggerType.Object => !isPlayer,
				_ => true
			};
		}

		private void FixedUpdate()
		{
			if (Context == null || !Context.IsServer) return;
			if (_colliders.Count == 0 && !Context.HasFlag(_flag)) return;

			_colliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

			bool pressed = CheckWeight() >= _weightToBeActivated;
			bool wasPressed = Context.HasFlag(_flag);
			if (pressed == wasPressed) return;
			Context.SetFlag(_flag, pressed);
			if (pressed) Raise(new LDActivation(null, transform.position));

			Debug.Log($"[LD] Plaque : {(pressed ? "pressée" : "relâchée")} (poids {CheckWeight()})");
		}

		private float CheckWeight()
		{
			_occupants.Clear();
			foreach (Collider col in _colliders)
			{
				NetworkObject netObj = col.GetComponentInParent<NetworkObject>();
				if (netObj != null) _occupants.Add(netObj);
			}

			float total = 0f;
			foreach (NetworkObject netObj in _occupants)
			{
				if (netObj.IsPlayerObject)
				{
					total += _playerWeightValue;
					PlayerInventory playerInv = netObj.GetComponentInChildren<PlayerInventory>();
					if (playerInv != null)
					{
						for(int i = 0; i < playerInv.ItemsInInventory.Length; i++)
						{
							ItemInstance item = playerInv.ItemsInInventory[i];
							if(item != null) 
								total += item.Data.Weight;
						}
					}
				}
				else if (netObj.TryGetComponent(out ItemPickup pickup) && pickup.Instance != null)
					total += pickup.Instance.Data.Weight;
			}
			return total;
		}
	}
}