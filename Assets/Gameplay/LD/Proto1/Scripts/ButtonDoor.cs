using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class ButtonDoor : OnlineInteractable
{
    [SerializeField] float activeDuration = 5f;
    public NetworkVariable<bool> activated = new(false);
    public event Action StateChanged;

    Coroutine _routine;

    protected override void ServerInteract(ulong sender)
    {
        if (_routine != null) StopCoroutine(_routine);

        activated.Value = true;
        OpenEventsRpc();
        StateChanged?.Invoke();

        _routine = StartCoroutine(WaitToDeactivate());
    }

    IEnumerator WaitToDeactivate()
    {
        yield return new WaitForSeconds(activeDuration);
        activated.Value = false;
        _routine = null;
        CloseEventsRpc();
        StateChanged?.Invoke();
    }
}