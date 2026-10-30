using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class ManorBuilderNetwork : NetworkBehaviour
    {
        [SerializeField] ManorBuilder manorBuilder;
        [SerializeField] NetworkObject doorPrefab;

        private readonly NetworkVariable<int> _manorSeed = new NetworkVariable<int>(0);
        private readonly NetworkVariable<bool> _seedReady = new NetworkVariable<bool>(false);
        private readonly List<NetworkObject> _spawnedDoors = new List<NetworkObject>();

        public override void OnNetworkSpawn()
        {
            if (manorBuilder == null) manorBuilder = GetComponent<ManorBuilder>();

            if (IsServer)
            {
                _manorSeed.Value = Random.Range(int.MinValue, int.MaxValue);
                _seedReady.Value = true;

                manorBuilder.Build(_manorSeed.Value);
                SpawnDoors();
                return;
            }
            
            if (_seedReady.Value)
            {
                manorBuilder.Build(_manorSeed.Value);
            }
            else
            {
                _seedReady.OnValueChanged += OnSeedReadyChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            _seedReady.OnValueChanged -= OnSeedReadyChanged;
        }

        private void OnSeedReadyChanged(bool oldValue, bool newValue)
        {
            if (!newValue) return;
            manorBuilder.Build(_manorSeed.Value);
        }

        private void SpawnDoors()
        {
            if (!IsServer) return;
            if (doorPrefab == null) return;
            if (manorBuilder.Result == null || manorBuilder.Result.doors == null) return;

            Vector3[] worldDirections =
            {
                Vector3.forward,
                Vector3.right,
                Vector3.back,
                Vector3.left
            };

            foreach (DoorConnection connection in manorBuilder.Result.doors)
            {
                Vector3 positionA = manorBuilder.CellToWorld(connection.cellA);
                Vector3 positionB = manorBuilder.CellToWorld(connection.cellB);
                Vector3 doorPosition = (positionA + positionB) * 0.5f + new Vector3(0, doorPrefab.GetComponentInChildren<Renderer>().bounds.extents.y * 0.5f + 0.5f,0) ;

                Quaternion doorRotation = Quaternion.LookRotation(worldDirections[connection.direction]) * Quaternion.Euler(0, -90, 0);

                NetworkObject door = Instantiate(doorPrefab, doorPosition, doorRotation);
                door.Spawn(true);
                _spawnedDoors.Add(door);
            }
        }

        public void DespawnDoors()
        {
            if (!IsServer) return;

            foreach (NetworkObject door in _spawnedDoors)
            {
                if (door != null && door.IsSpawned) door.Despawn();
            }
            _spawnedDoors.Clear();
        }
    }
}