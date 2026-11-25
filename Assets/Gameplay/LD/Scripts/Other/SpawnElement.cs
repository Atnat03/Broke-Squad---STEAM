using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.LD
{
    public class SpawnElement : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _objectToSpawn;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private bool _awakeSpawn = false;
        NetworkObject _currentInstance;

        public override void OnNetworkSpawn()
        {
            if (_awakeSpawn)
            {
                SpawnObject();
            }
        }

        public void SpawnObject()
        {
            if (!IsServer) return;

            if (_currentInstance != null)
            {
                _currentInstance.Despawn();
                Destroy(_currentInstance);
                _currentInstance = null;
            }
            
            _currentInstance = Instantiate(_objectToSpawn, _spawnPoint);
            _currentInstance.Spawn();
        }
    }
}