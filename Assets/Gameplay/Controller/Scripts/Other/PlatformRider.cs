using Gameplay.Controller;
using Gameplay.LD;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlatformRider : NetworkBehaviour
{
    [SerializeField] private float _remoteSmoothing = 15f;

    private MovingPlatform _currentPlatform;
    private PlayerController _playerController;
    
    private readonly NetworkVariable<ulong> _ridingPlatformId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<Vector3> _ridingLocalPos = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool _remoteWasRiding;
    private Vector3 _remoteSmoothedLocalPos;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        _playerController.Motor.PlatformVelocity = _currentPlatform != null ? _currentPlatform.CurrentVelocity : Vector3.zero;

        if (_currentPlatform != null)
        {
            _ridingPlatformId.Value = _currentPlatform.NetworkObjectId;
            _ridingLocalPos.Value = _currentPlatform.transform.InverseTransformPoint(transform.position);
        }
        else if (_ridingPlatformId.Value != 0)
        {
            _ridingPlatformId.Value = 0;
        }
    }
    
    private void LateUpdate()
    {
        if (!IsSpawned || IsOwner) return;

        MovingPlatform platform = FindPlatform(_ridingPlatformId.Value);

        if (platform == null)
        {
            _remoteWasRiding = false;
            return;
        }

        Transform platformT = platform.transform;

        if (!_remoteWasRiding)
        {
            _remoteSmoothedLocalPos = platformT.InverseTransformPoint(transform.position);
            _remoteWasRiding = true;
        }

        float k = 1f - Mathf.Exp(-_remoteSmoothing * Time.deltaTime);
        _remoteSmoothedLocalPos = Vector3.Lerp(_remoteSmoothedLocalPos, _ridingLocalPos.Value, k);

        transform.position = platformT.TransformPoint(_remoteSmoothedLocalPos);
    }

    private MovingPlatform FindPlatform(ulong id)
    {
        if (id == 0) return null;

        return NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject no) ? no.GetComponent<MovingPlatform>() : null;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        MovingPlatform platform = other.GetComponentInParent<MovingPlatform>();

        if (platform == null) return;

        _currentPlatform = platform;
    }

    public void OnTriggerExit(Collider other)
    {
        if (!IsOwner || _currentPlatform == null) return;

        MovingPlatform platform = other.GetComponentInParent<MovingPlatform>();

        if (platform != _currentPlatform) return;

        _currentPlatform = null;
    }
}