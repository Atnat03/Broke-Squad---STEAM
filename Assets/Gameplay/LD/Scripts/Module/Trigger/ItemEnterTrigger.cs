using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	[RequireComponent(typeof(Collider))]
	public class ItemEnterTrigger : LDTriggerModule
	{
		[Tooltip("0 = pas besoin de velocity")]
		[SerializeField, Min(0f)] private float _thresholdVelocity = 10f;

		protected override void OnInitialize()
		{
			GetComponent<Collider>().isTrigger = true;
			if (!TryGetComponent<Rigidbody>(out _))
				gameObject.AddComponent<Rigidbody>().isKinematic = true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (Context == null|| !Context.IsServer) return;

			ItemPickup item = other.GetComponent<ItemPickup>();
			if (item == null) return;

			if (!item.TryGetComponent(out Rigidbody rb)) return;
			if (rb.linearVelocity.magnitude < _thresholdVelocity) return;

			Raise(new LDActivation(item.NetworkObject, item.transform.position)); 
		}
	}
}
