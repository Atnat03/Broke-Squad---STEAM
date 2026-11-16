using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerActivateObjects : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToToggle;

    void Awake()
    {
        SetActivate(false);
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsLocalPlayer(other)) SetActivate(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsLocalPlayer(other)) SetActivate(false);
    }
    
    bool IsLocalPlayer(Collider other)
    {
        var netObj = other.GetComponentInParent<NetworkObject>();
        return netObj != null && netObj.IsLocalPlayer;
    }
    
    void SetActivate(bool active)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            obj.SetActive(active);
        }
    }
}
