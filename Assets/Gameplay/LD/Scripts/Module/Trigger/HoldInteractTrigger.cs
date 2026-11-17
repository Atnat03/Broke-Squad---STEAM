using Gameplay.Items;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	public class HoldInteractTrigger : LDTriggerModule, IHoldInteractable
	{

		[SerializeField] private float _range = 4f;

		[SerializeField] private LDFlag _flag = LDFlag.Activated;
		public Outline outline { get; private set; }

		[SerializeField] TooltipConfig _tooltipConfig;

		private readonly HashSet<ulong> _holders = new();


		protected override void OnInitialize()
		{
			gameObject.layer = Context.Element.gameObject.layer;
			GetComponent<Collider>().isTrigger = false;
			outline = Context.Element.GetComponentInChildren<Outline>(true);
			SetOutline(false);

			if (Context.IsServer)
				NetworkManager.Singleton.OnClientConnectedCallback += OnClientDisconnected;
		}

		public override void OnDestroy()
		{
			if (NetworkManager.Singleton != null)
				NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
			base.OnDestroy();
		}

		#region Interact réseau 

		public void HoldStart(ulong clientId) => RequestHoldInteractServerRpc(true);
		public void HoldEnd(ulong clientId) => RequestHoldInteractServerRpc(false);

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
		private void RequestHoldInteractServerRpc(bool pressed, ServerRpcParams rpcParams = default)
		{
			ulong sender = rpcParams.Receive.SenderClientId;

			if (pressed)
			{
				if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;
				if (client.PlayerObject == null) return;
				if (Vector3.Distance(client.PlayerObject.transform.position, transform.position) > _range) return;
				if (!_holders.Add(sender)) return;
				Refresh(client.PlayerObject);
			}
			else if (_holders.Remove(sender))
			{
				Refresh(null);
			}
		}

		private void OnClientDisconnected(ulong clientId)
		{
			if (_holders.Remove(clientId)) Refresh(null);
		}

		private void Refresh(NetworkObject player)
		{
			bool held = _holders.Count > 0;
			if (held == Context.HasFlag(_flag)) return;

			Context.SetFlag(_flag, held);
			if (held)
				Raise(new LDActivation(player, player != null ? player.transform.position : transform.position));

		}

		public void Interact(ulong clientId) { }
		#endregion

		#region Outline

		public string GetTooltip(ItemModule[] playerModules)
		{
			return _tooltipConfig.GetTooltip(playerModules);
		}

		public void SetOutline(bool state)
		{
			if (outline != null) outline.enabled = state;
		}

		#endregion
	}
}
