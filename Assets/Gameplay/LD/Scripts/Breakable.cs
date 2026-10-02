using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Breakable : NetworkBehaviour
{
    [SerializeField] private GameObject _normalMesh;
    [SerializeField] private float solidity = 20f;
    [SerializeField] private bool _dropPickableItem = false;

    [Header("Sans drop d'item")]
    [SerializeField] private Rigidbody[] _breakMesh;

    [Header("Avec drop d'item")]
    [SerializeField] private NetworkObject _dropItemPrefabs;
    [SerializeField] private Transform[] _pickupSpawnPoints;

    public override void OnNetworkSpawn()
    {
        _normalMesh.SetActive(true);

        foreach (var rb in _breakMesh)
            rb.gameObject.SetActive(false);
    }

    public void Break(float force)
    {
        if (force < solidity) return;
        AskServerToBreakRpc(force);
    }

    [Rpc(SendTo.Server)]
    private void AskServerToBreakRpc(float force)
    {
        float radius = transform.localScale.magnitude;

        if (_dropPickableItem)
        {
            for (int i = 0; i < _pickupSpawnPoints.Length; i++)
            {
                Transform p = _pickupSpawnPoints[i];
                NetworkObject obj = Instantiate(_dropItemPrefabs, p.position, p.rotation);
                obj.Spawn();

                if (obj.TryGetComponent(out Rigidbody rb))
                    rb.AddExplosionForce(force, transform.position, radius);
            }
        }

        ReplicateBreakRpc(force);
    }

    [Rpc(SendTo.Everyone)]
    private void ReplicateBreakRpc(float force)
    {
        _normalMesh.SetActive(false);

        if (_dropPickableItem) return;

        float radius = transform.localScale.magnitude;
        foreach (var rb in _breakMesh)
        {
            rb.gameObject.SetActive(true);
            rb.AddExplosionForce(force, transform.position, radius);
        }
    }
}