using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class InteractTrigger : LDTriggerModule, IInteractable
    {
        private float _range;

        public Outline outline { get; private set; }

        protected override void OnInitialize(ModuleConfig config)
        {
            InteractTriggerConfig c = (InteractTriggerConfig)config;
            _range = c.Range;

            gameObject.layer = Context.Element.gameObject.layer;

            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = c.Center;
            box.size = c.Size;

            outline = Context.Element.GetComponentInChildren<Outline>(true);
            SetOutline(false);
        }

        public void SetOutline(bool state)
        {
            if (outline != null) outline.enabled = state;
        }

        public void Interact(ulong clientId) => Context.RequestInteract(ModuleID);

        public void ServerInteract(NetworkObject player)
        {
            if (!Context.IsServer) return;
            if (Vector3.Distance(player.transform.position, transform.position) > _range) return;
            Debug.Log("[LD] interact ok");
            Raise(new LDActivation(player, player.transform.position));
        }
    }
}