using System;
using Unity.Netcode;

public class LeverDoor : OnlineInteractable
{
    public NetworkVariable<bool> activated = new(false);
    public event Action StateChanged; 

    protected override void ServerInteract(ulong sender)
    {
        activated.Value = !activated.Value;
        if (activated.Value) OpenEventsRpc(); else CloseEventsRpc();
        StateChanged?.Invoke();
    }
}