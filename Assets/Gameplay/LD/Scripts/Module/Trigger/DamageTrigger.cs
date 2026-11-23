using Gameplay.LD.Scripts;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class DamageTrigger : LDTriggerModule, IDamageable, IBreakable
    {
        [Tooltip("Seuil mini des dégats pour être compter")]
        [SerializeField] private float _damageTreshold = 10f;

        [Tooltip("PV de l'objet, 0 = se brise en 1 coup")]
        [SerializeField] private float _health = 20;

		[SerializeField, Min(0f)] private float _range = 6f;

		private float _currentHealth;
        protected override void OnInitialize()
        {
            _currentHealth = _health;
        }

        public void ApplyDamage(float damage, NetworkObject source = null)
        {
            if (!Context.IsServer) return;
            if (damage < _damageTreshold) return;

            if (_health > 0)
            {
                if (_currentHealth <= 0f) return;
                _currentHealth -= damage;
                if (_currentHealth > 0f) return;
            }

            Debug.Log("[LD] damage detecté");

            Raise(new LDActivation(null, transform.position));
        }

        public void Break(float force) => BreakServeurRpc(force);


		[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
		private void BreakServeurRpc(float force, RpcParams rpcParams = default)
		{
			if (Context == null) return;

			ulong sender = rpcParams.Receive.SenderClientId;

			if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;

			NetworkObject player = client.PlayerObject;

			if (player == null) return;
			if (Vector3.Distance(player.transform.position, transform.position) > _range) return;

            ApplyDamage(force, player);
		}
	}
}
