using Gameplay.Controller;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerReviveInteractable : MonoBehaviour, IInteractable, IHoldInteractable
    {
        [SerializeField] private PlayerRevive _revive; 
        [SerializeField] private string _tooltipMessage = "Hold to revive";
        [SerializeField] private Outline _outline;

        public Tooltip GetTooltip(ItemModule[] modules)
        {
            var nm = NetworkManager.Singleton;
            bool can = nm != null && CanRevive(nm.LocalClientId);
            return new Tooltip { Message = can ? _tooltipMessage : string.Empty, Icon = null };
        }

        public Outline outline => _outline;

        public void Interact(ulong clientId) { } 

        public void HoldStart(ulong clientId)
        {
            if (CanRevive(clientId)) _revive.LocalHoldStart();
        }

        public void HoldEnd(ulong clientId)
        {
            if (_revive != null) _revive.LocalHoldEnd();
        }

        public void SetOutline(StartOutliningData data)
        {
            outline.enabled = data.State;
        }

        private bool CanRevive(ulong clientId)
        {
            if (_revive == null || !_revive.IsSpawned || !_revive.IsDowned) return false;
            if (_revive.OwnerClientId == clientId) return false;         
            
            if (!PlayerRevive.TryGet(clientId, out var me) || me.IsDowned) return false;

            return true;
        }
    }
}