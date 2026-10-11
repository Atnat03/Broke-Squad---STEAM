

using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class InteractTrigger : LDTriggerModule, IInteractable
    {
        private float _range;

        protected override void OnInitialize(ModuleConfig config)
        {
            InteractTriggerConfig interactConfig = config as InteractTriggerConfig;
            _range = interactConfig.Range;

            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = interactConfig.Center;
            box.size = interactConfig.Size;
        }

        public void Interact() => Context.RequestInteract(ModuleID);

        public void ServerInteract(NetworkObject player)
        {
            Debug.Log("[LD] interact ok");
            if (!Context.IsServer) return;
            if (Vector3.Distance(player.transform.position, transform.position) > _range) return;
            Raise(new LDActivation(player, player.transform.position));
        }
    }
}