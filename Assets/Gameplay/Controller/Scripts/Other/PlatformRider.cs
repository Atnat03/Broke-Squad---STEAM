using Gameplay.LD;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlatformRider : NetworkBehaviour
{
    private MovingPlatform _currentPlatform;
    private Vector3 _lastPlatformPos;
    
    public void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.gameObject.TryGetComponent(out MovingPlatform platform))
        {
            SetPlatform(platform);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.gameObject.TryGetComponent(out MovingPlatform platform))
        {
            _currentPlatform = null;
        }
    }
    
    public void SetPlatform(MovingPlatform platform)
    {
        if (!IsOwner) return;
        
        _currentPlatform = platform;
        if (platform != null)
            _lastPlatformPos = platform.transform.position;
    }

    private void LateUpdate()
    {
        if (!IsOwner || _currentPlatform == null) return;

        Vector3 delta = _currentPlatform.transform.position - _lastPlatformPos;
        
        if (delta.sqrMagnitude > 0.00001f)
        {
            transform.position += delta;
        }
        
        _lastPlatformPos = _currentPlatform.transform.position;
    }
}