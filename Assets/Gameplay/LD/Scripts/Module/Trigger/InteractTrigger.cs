using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    [RequireComponent(typeof(Collider))]
    public class InteractTrigger : LDTriggerModule, IInteractable
    {
        [SerializeField] private float _range = 4f;

        public Outline outline { get; private set; }

        protected override void OnInitialize()
        {
            gameObject.layer = Context.Element.gameObject.layer;
            GetComponent<Collider>().isTrigger = false;
            outline = Context.Element.GetComponentInChildren<Outline>(true);
            SetOutline(false);
        }

        public void SetOutline(bool state)
        {
            if (outline != null) outline.enabled = state;
        }

        public void Interact(ulong clientId) => Context.RequestInteract(this);

        public void ServerInteract(NetworkObject player)
        {
            if (!Context.IsServer) return;
            if (Vector3.Distance(player.transform.position, transform.position) > _range) return;
            Debug.Log("[LD] interact");
            Raise(new LDActivation(player, player.transform.position));
        }
    }
}