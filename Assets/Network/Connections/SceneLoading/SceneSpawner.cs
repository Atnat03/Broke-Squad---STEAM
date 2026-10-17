using System.Collections.Generic;
using Gameplay.Controller;
using Network.Connections.SceneLoading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        for (int i = 0; i < clientsCompleted.Count; i++)
        {
            ulong clientId = clientsCompleted[i];
            
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) continue;

            var point = SpawnPointManager.instance.GetPlayerSpawnPoint(i);
            var player = Instantiate(playerPrefab, point.position, point.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            player.GetComponent<PlayerController>().TeleportClientRpc(point.position, point.rotation);
        }
    }
}