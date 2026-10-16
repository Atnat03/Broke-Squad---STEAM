using Gameplay;
using UnityEngine;

public class LeverDoor : OnlineInteractable
{
    public bool activated = false;
    
    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);
        
        activated = !activated;
        if(activated) onInteractableOpen?.Invoke();
        else onInteractableClose?.Invoke();
    }
}
