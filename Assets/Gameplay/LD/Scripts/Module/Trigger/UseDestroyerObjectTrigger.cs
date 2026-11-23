using Gameplay.Items;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	[RequireComponent(typeof(Collider))]
	public class UseDestroyerObjectTrigger : LDTriggerModule, IBreakable
	{
		[SerializeField, Min(0f)] private float _thresholdSolidity = 10f;
		[SerializeField, Min(0f)] private float _range = 4f;

		protected override void OnInitialize()
		{
			GetComponent<Collider>().isTrigger = true;
		}
		public void Break(float force) => BreakServeurRpc(force);

		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
		private void BreakServeurRpc(float force, RpcParams rpcParams= default)
		{
			if (Context == null || force < _thresholdSolidity) return;

			ulong sender = rpcParams.Receive.SenderClientId;

			if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;

			NetworkObject player = client.PlayerObject;

			if (player == null) return;
			if (Vector3.Distance(player.transform.position, transform.position) > _range) return;

			Raise(new LDActivation(player, player.transform.position));
		}
	}
}

