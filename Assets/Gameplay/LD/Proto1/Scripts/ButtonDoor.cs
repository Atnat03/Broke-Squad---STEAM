using System.Collections;
using UnityEngine;

public class ButtonDoor : OnlineInteractable
{
    [SerializeField] float activeDuration = 5f;
    public bool activated = false;
    private Coroutine _waitToDeactivateCoroutine;
    
    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);
        
        StopCoroutine(_waitToDeactivateCoroutine);
        activated = true;
        onInteractableOpen?.Invoke();
        _waitToDeactivateCoroutine = StartCoroutine(WaitToDeactivate());
    }
    
    IEnumerator WaitToDeactivate()
    {
        yield return new WaitForSeconds(activeDuration);
        activated = false;
        onInteractableClose?.Invoke();
    }
}
