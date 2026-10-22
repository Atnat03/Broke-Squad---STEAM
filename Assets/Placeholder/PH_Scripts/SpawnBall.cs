using Unity.Netcode;
using UnityEngine;
using Gameplay.Items;

public class SpawnBall : NetworkBehaviour
{
    [SerializeField] private NetworkObject _ballPrefab;
    [SerializeField] private Material _staticTV;
    [SerializeField] private NetworkObject _TV;
    public void OnTriggerEnter(Collider other)
    {
        if(!IsServer) return;
        
        if (other.gameObject.CompareTag("WeBalling"))
        {
            NetworkObject n = Instantiate(_ballPrefab, transform.position, transform.rotation);
            n.Spawn();
            other.GetComponent<NetworkObject>().Despawn();

            ReplicateScreenChangeRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ReplicateScreenChangeRpc()
    {
        _TV.gameObject.GetComponent<Renderer>().material = _staticTV;
    }
}
