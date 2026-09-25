using MyPrint;
using UnityEngine;
using UnityEngine.Events;

public class TriggerTextExemple : MonoBehaviour
{
    public UnityEvent onTrigger;
    
    [ContextMenu("Trigger Effect")]
    public void TestTrigger()
    {
        ABPrint.Print("Trigger Effect");
        onTrigger?.Invoke();
    }
}
