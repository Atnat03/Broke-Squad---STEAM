using UnityEngine;

public class PlayerVisualLobbySpawner : MonoBehaviour
{
    [SerializeField] private Transform[] _spawnPoints;
    
    public Transform GetSpawnPoint(int id) => _spawnPoints[id];
}
