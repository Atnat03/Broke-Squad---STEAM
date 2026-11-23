using System;
using Unity.Netcode;
using UnityEngine;

public class PlatformRider : NetworkBehaviour
{
    [SerializeField] private LayerMask _platformMask;
    [SerializeField] private float _rayStartHeight = 0.2f;
    [SerializeField] private float _rayLength = 0.5f;

    private readonly NetworkVariable<ulong> _platformId = new();

    private ulong _appliedId; 

    private void Update()
    {
        if (!IsSpawned) return;
        
        if (_appliedId != _platformId.Value) TryApplyParent(_platformId.Value);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        
        ulong foundId = 0;

        NetworkObject platform = other.gameObject.GetComponent<NetworkObject>();
        if (platform != null) foundId = platform.NetworkObjectId;

        if (foundId == _appliedId) return;

        if (TryApplyParent(foundId))
            SetPlatformServerRpc(foundId);
    }

    private bool TryApplyParent(ulong id)
    {
        if (id == 0)
        {
            transform.SetParent(null, true);
            _appliedId = 0;
            return true;
        }

        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject platform))
        {
            transform.SetParent(platform.transform, true);
            _appliedId = id;
            return true;
        }

        return false;
    }

    [ServerRpc]
    private void SetPlatformServerRpc(ulong id) => _platformId.Value = id;
}