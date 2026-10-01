using System;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

public class Breakable : NetworkBehaviour
{
    [SerializeField] private GameObject _normalMesh;
    [SerializeField] private Rigidbody[] _breakMesh;
    [SerializeField] private bool _dropPickableItem = false;

    public override void OnNetworkSpawn()
    {
        _normalMesh.SetActive(true);

        foreach (var rb in _breakMesh)
            rb.gameObject.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    void AskServerToBreakRpc(float force)
    {
        if (_dropPickableItem)
        {
            _normalMesh.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            ReplicateBreakRpc(force);
        }
    }

    [Rpc(SendTo.Everyone)]
    void ReplicateBreakRpc(float force)
    {
        _normalMesh.SetActive(false);
        
        ABPrint.Print("Break the wall");
        
        foreach (var rb in _breakMesh)
        {
            rb.gameObject.SetActive(true);
            
            rb.AddExplosionForce(force, transform.position, transform.localScale.magnitude);
        }
    }
    
    public void Break(float force)
    {
        AskServerToBreakRpc(force);
    }
}
