using System.Collections.Generic;
using Gameplay.LD.Proto1;
using Gameplay.LD.Proto1.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Door : OnlineInteractable
{
    public enum FaceAxis { Forward, Right, Up }
    
    [Header("Interactables Dependents")]
    [SerializeField] List<LeverDoor> RequiredLevers;
    [SerializeField] List<ButtonDoor> RequiredButtons;
    [SerializeField] bool canBeInteracted;
    [SerializeField] bool startLocked;
    
    [Header("Sound")]
    [SerializeField, SoundName] private string openSound;
    [SerializeField, SoundName] private string closeSound;
    
    [Header("Open side")]
    [SerializeField] FaceAxis faceAxis = FaceAxis.Right;
    [SerializeField] bool invertSide;
    public UnityEvent onOpenFront; 
    public UnityEvent onOpenBack;
    Transform Ref => transform;
    
    readonly NetworkVariable<bool> _locked = new(false);
    readonly NetworkVariable<int> _state = new(0);

    KeyCondition _keyCondition;

    void Awake()
    {
        _keyCondition = GetComponent<KeyCondition>();
    }

    public override void OnNetworkSpawn()
    {
        _state.OnValueChanged += OnStateChanged;

        if (!IsServer) return;
        _locked.Value = startLocked;
        foreach (var l in RequiredLevers) l.StateChanged += Evaluate;
        foreach (var b in RequiredButtons) b.StateChanged += Evaluate;
    }

    public override void OnNetworkDespawn()
    {
        _state.OnValueChanged -= OnStateChanged;

        if (!IsServer) return;
        foreach (var l in RequiredLevers) l.StateChanged -= Evaluate;
        foreach (var b in RequiredButtons) b.StateChanged -= Evaluate;
    }
    
    void OnStateChanged(int previous, int current)
    {
        Debug.Log($"OnStateChanged {previous} {current}");
        
        
        if (current == 0)
        {
            onInteractableClose?.Invoke();
            InvokeEvent(new PlaySoundEvent
            {
                soundName = closeSound,
                position = transform.position,
                volume = 0.8f,
                pitch = Random.Range(0.95f, 1.05f) 
            });
            return;
        }

        InvokeEvent(new PlaySoundEvent
        {
            soundName = openSound,
            position = transform.position,
            volume = 0.8f,
            pitch = Random.Range(0.95f, 1.05f) 
        });
        if (current > 0) onOpenFront?.Invoke();
        else onOpenBack?.Invoke();

        onInteractableOpen?.Invoke(); 
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

        SetOpen(_state.Value == 0, GetSide(sender));
    }

    protected override void OnInteract()
    {
        ABPrint.Print("canBeInteracted");

        if(!canBeInteracted) return;
        
        ABPrint.Print("_locked");

        
        if (_locked.Value) return;
        
        ABPrint.Print("OnInteract");

        SetOpen(_state.Value == 0);
    }

    void SetOpen(bool open, int side = 1)
    {
        bool isOpen = _state.Value != 0;
        if (isOpen == open) return;
        _state.Value = open ? side : 0;
    }

    int GetSide(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            return 1;

        Vector3 toPlayer = client.PlayerObject.transform.position - Ref.position;
        toPlayer.y = 0f;

        int side = Vector3.Dot(FaceNormal, toPlayer) >= 0f ? 1 : -1;
        return invertSide ? -side : side;
    }
    
    public bool TryUnlock(Vector2Int range)
    {
        if (!IsServer)
            return false;
        
        if (!_locked.Value) return false;
        
        if (_keyCondition.TryOpen(range))
        {
            _locked.Value = false;
            SetOpen(true);
            return true;
        }

        return false;
    }
    
    Vector3 FaceNormal => faceAxis switch
    {
        FaceAxis.Right => Ref.right,
        FaceAxis.Up => Ref.up,
        _ => Ref.forward
    };
}