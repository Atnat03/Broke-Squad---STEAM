using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Door : OnlineInteractable
{
    [SerializeField] List<LeverDoor> RequiredLevers;
    [SerializeField] List<ButtonDoor> RequiredButtons;

    readonly NetworkVariable<bool> _locked = new(true);   // starts locked
    readonly NetworkVariable<bool> _opened = new(false);

    public override void OnNetworkSpawn()
    {
        // Every machine plays the visuals when the synced state changes
        _opened.OnValueChanged += (_, open) =>
        {
            if (open) onInteractableOpen?.Invoke();
            else onInteractableClose?.Invoke();
        };

        if (!IsServer) return;
        foreach (var l in RequiredLevers) l.StateChanged += Evaluate;
        foreach (var b in RequiredButtons) b.StateChanged += Evaluate;
    }

    protected override void ServerInteract(ulong sender)
    {
        if (_locked.Value) return;
        _opened.Value = !_opened.Value;
    }

    void Evaluate()
    {
        bool allActive = true;
        foreach (var l in RequiredLevers) allActive &= l.activated.Value;
        foreach (var b in RequiredButtons) allActive &= b.activated.Value;

        _locked.Value = !allActive;
        _opened.Value = allActive;   // auto-open on unlock, auto-close on lock
    }
}