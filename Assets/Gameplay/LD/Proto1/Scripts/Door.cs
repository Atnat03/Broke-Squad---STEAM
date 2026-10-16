using System.Collections.Generic;
using UnityEngine;

public class Door : OnlineInteractable
{
    [SerializeField] List<LeverDoor> RequiredLevers;
    [SerializeField] List<ButtonDoor> RequiredButtons;
    private  bool _locked;
    private bool _opened;

    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);

        if (!_locked)
        {
            if(!_opened) onInteractableOpen.Invoke();
            else onInteractableClose.Invoke();
        }
    }

    public void TryUnlock()
    {
        foreach (LeverDoor lever in RequiredLevers)
        {
            if (!lever.activated) return;
        }

        foreach (ButtonDoor button in RequiredButtons)
        {
            if (!button.activated) return;
        }
        
        _locked = false;
        _opened = true;
        onInteractableOpen?.Invoke();
    }

    public void TryLock()
    {
        _locked = true;
        _opened = false;
        onInteractableClose?.Invoke();
        
    }
}
