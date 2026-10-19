using System;
using UnityEngine;

namespace Network.Connections.SceneLoading
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager instance;

        [Serializable]
        public struct PlayerSpawnOptions
        {
            public Transform[] points;  
        }
        
        [SerializeField] private PlayerSpawnOptions[] players;

        void Awake() => instance = this;

        public Transform GetPlayerSpawnPoint(int playerIndex)
        {
            var options = players[playerIndex % players.Length].points;
            return options[UnityEngine.Random.Range(0, options.Length)];
        }
    }
}