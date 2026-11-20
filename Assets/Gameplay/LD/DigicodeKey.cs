using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	[RequireComponent(typeof(Collider))]
	public class DigicodeKey : NetworkBehaviour, IInteractable
	{
		public enum KeyType { Digit, Clear, Confirm }

		[SerializeField] private DigicodeTrigger _keypad;
		[SerializeField] private KeyType _type = KeyType.Digit;
		[SerializeField, Range(0, 9)] private int _digit;
		[SerializeField] private Outline _outline;
		[SerializeField] TooltipConfig _tooltipConfig;

		public Outline outline => _outline;

		private void Awake()
		{
			var element = GetComponentInParent<LDElement>();
			if (element != null) gameObject.layer = element.gameObject.layer;
			GetComponent<Collider>().isTrigger = false;
			SetOutline(new StartOutliningData
			{
				State = false
			});
		}

		public void SetOutline(StartOutliningData data)
		{
			if (_outline != null) _outline.enabled = data.State;
		}

		public void Interact(ulong clientId)
		{
			int key = _type switch
			{
				KeyType.Clear => DigicodeTrigger.ClearKey,
				KeyType.Confirm => DigicodeTrigger.ConfirmKey,
				_ => _digit
			};
			PressServerRpc(key);
		}

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
		private void PressServerRpc(int key, RpcParams rpcParams = default)
		{
			if (_keypad == null) return;
			ulong sender = rpcParams.Receive.SenderClientId;
			if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;
			if (client.PlayerObject == null) return;
			_keypad.ServerPress(client.PlayerObject, key);
		}

		public Tooltip GetTooltip(ItemModule[] playerModules)
		{
			return _tooltipConfig.GetTooltip(playerModules); ;
		}
	}
}