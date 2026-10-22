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
        if (other.gameObject.CompareTag("WeBalling"))
        {
            _ballPrefab.Spawn();
            Destroy(other.gameObject);
            _TV.gameObject.GetComponent<Renderer>().material = _staticTV;
        }
    }
}
