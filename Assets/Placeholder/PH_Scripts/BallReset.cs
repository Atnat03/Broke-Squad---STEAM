using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Remet la boule à sa position de départ pour tout le monde.
/// Appeler ResetBall() (UnityEvent) depuis le serveur ou un client.
/// </summary>
public class BallReset : NetworkBehaviour
{
    private Vector3 _startPos;
    private Quaternion _startRot;
    private Rigidbody _rb;
    private NetworkTransform _netTransform;

    private void Awake()
    {
        _startPos = transform.position;
        _startRot = transform.rotation;
        _rb = GetComponent<Rigidbody>();
        _netTransform = GetComponent<NetworkTransform>();
    }

    public void ResetBall()
    {
        if (IsServer) DoReset();
        else ResetBallServerRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void ResetBallServerRpc() => DoReset();

    private void DoReset()
    {
        StopRigidbody();
        if (_rb != null) { _rb.position = _startPos; _rb.rotation = _startRot; }

        if (_netTransform != null) _netTransform.Teleport(_startPos, _startRot, transform.localScale);
        else transform.SetPositionAndRotation(_startPos, _startRot);

        StopRigidbodyClientRpc();
    }

    [Rpc(SendTo.NotServer)]
    private void StopRigidbodyClientRpc() => StopRigidbody();

    private void StopRigidbody()
    {
        if (_rb == null || _rb.isKinematic) return;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
