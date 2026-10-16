using System.Collections.Generic;
using Gameplay.LD.Proto1.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

public class Door : OnlineInteractable
{
    [SerializeField] List<LeverDoor> RequiredLevers;
    [SerializeField] List<ButtonDoor> RequiredButtons;
    [SerializeField] private bool _locked = true;
    [SerializeField] private bool _opened;
    
    private KeyCondition _keyCondition;

    void Awake()
    {
        _keyCondition = GetComponent<KeyCondition>();
    }

    public override void Interact(ulong clientId)
    {
        if (!IsServer)
        {
            base.Interact(clientId);
            return;
        }

        if (!_locked)
            TryOpen();
    }
    
    void TryOpen()
    {
        ABPrint.Print("TryOpen");
        
        _opened = !_opened;
            
        if(_opened) 
            onInteractableOpen.Invoke();
        else 
            onInteractableClose.Invoke();
    }
    
    public bool TryUnlock(Vector2Int range)
    {
        if (!IsServer)
        {
            TryUnlockRpc(range.x, range.y);
            return false;
        }
        
        if (!_locked) return false;
        
        if (_keyCondition.TryOpen(range))
        {
            _locked = false;
            TryOpen();
            return true;
        }
        
        return false;
    }
    
    [Rpc(SendTo.Server)]
    private void TryUnlockRpc(int rangeX, int rangeY) => TryUnlock(new Vector2Int(rangeX, rangeY));

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
