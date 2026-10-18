using System.Collections.Generic;
using Gameplay.LD.Proto1.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

public class Door : OnlineInteractable
{
    [SerializeField] List<LeverDoor> RequiredLevers;
    [SerializeField] List<ButtonDoor> RequiredButtons;
    [SerializeField] bool canBeInteracted;
    [SerializeField] bool startLocked;
    
    readonly NetworkVariable<bool> _locked = new(false);
    readonly NetworkVariable<bool> _opened = new(false);

    KeyCondition _keyCondition;

    void Awake()
    {
        _keyCondition = GetComponent<KeyCondition>(); 
    }

    public override void OnNetworkSpawn()
    {
        _opened.OnValueChanged += OnOpenedChanged;

        if (!IsServer) return;
        _locked.Value = startLocked;
        foreach (var l in RequiredLevers) l.StateChanged += Evaluate;
        foreach (var b in RequiredButtons) b.StateChanged += Evaluate;
    }

    public override void OnNetworkDespawn()
    {
        _opened.OnValueChanged -= OnOpenedChanged;

        if (!IsServer) return;
        foreach (var l in RequiredLevers) l.StateChanged -= Evaluate;
        foreach (var b in RequiredButtons) b.StateChanged -= Evaluate;
    }
    
    void OnOpenedChanged(bool _, bool open)
    {
        if (open) onInteractableOpen?.Invoke();
        else onInteractableClose?.Invoke();
    }

    void Evaluate()
    {
        bool allActive = true;
        foreach (var l in RequiredLevers) allActive &= l.activated.Value;
        foreach (var b in RequiredButtons) allActive &= b.activated.Value;

        bool shouldBeLocked = !allActive;
        if (_locked.Value == shouldBeLocked) return; 

        _locked.Value = shouldBeLocked;
        SetOpen(allActive); 
    }

    protected override void ServerInteract(ulong sender)
    {
        if(!canBeInteracted) return;
        if (_locked.Value) return;

        SetOpen(!_opened.Value);
    }

    protected override void OnInteract()
    {
        if(!canBeInteracted) return;
        if (_locked.Value) return;

        SetOpen(!_opened.Value);
    }

    void SetOpen(bool open)
    {
        if (_opened.Value == open) return;
        _opened.Value = open;
    }

    public bool TryUnlock(Vector2Int range)
    {
        if (!IsServer)
        {
            TryUnlockRpc(range.x, range.y);
            return false;
        }

        if (!_locked.Value) return false;

        if (_keyCondition.TryOpen(range))
        {
            _locked.Value = false;
            SetOpen(true);
            return true;
        }

        return false;
    }

    [Rpc(SendTo.Server)]
    private void TryUnlockRpc(int rangeX, int rangeY) => TryUnlock(new Vector2Int(rangeX, rangeY));
    
    
}