using UnityEngine.Events;

namespace Gameplay.LD.Proto1
{
    public class DigicodeButton : OnlineInteractable
    {
        public UnityEvent onInteractEvent;
        protected override void ServerInteract(ulong senderClientId)
        {
            onInteractEvent?.Invoke();
        }

        protected override void OnInteract()
        {
            return;
        }
    }
}