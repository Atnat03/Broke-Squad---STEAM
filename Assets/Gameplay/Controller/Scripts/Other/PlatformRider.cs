using System;
using Gameplay.Controller;
using Gameplay.LD;
using Unity.Netcode;
using UnityEngine;

public class PlatformRider : NetworkBehaviour
{
    [SerializeField] private LayerMask _platformMask;
    [SerializeField] private float _rayStartHeight = 0.2f;
    [SerializeField] private float _rayLength = 0.5f;

    private MovingPlatform _currentPlatform;
    private PlayerController _playerController;
    
    private readonly NetworkVariable<ulong> _platformId = new();

    private ulong _appliedId; 

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        if (!IsSpawned) return;

        if (_appliedId != _platformId.Value)
            TryApplyParent(_platformId.Value);

        if (!IsOwner) return;

        _playerController.Motor.PlatformVelocity = _currentPlatform != null ? _currentPlatform.CurrentVelocity : Vector3.zero;
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
    
    public void OnTriggerExit(Collider other)
    {
        if (!IsOwner || _currentPlatform == null)
            return;

        MovingPlatform platform = other.GetComponentInParent<MovingPlatform>();

        if (platform != _currentPlatform)
            return;

        SetPlatformServerRpc(0);

        _currentPlatform = null;
        _appliedId = 0;
    }

    private bool TryApplyParent(ulong id)
    {
        if (id == 0)
        {
            _appliedId = 0;
            _currentPlatform = null;
            return true;
        }

        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject platform))
        {
            _currentPlatform = platform.GetComponent<MovingPlatform>();

            _appliedId = id;
            return true;
        }

        return false;
    }

    [ServerRpc]
    private void SetPlatformServerRpc(ulong id) => _platformId.Value = id;
}