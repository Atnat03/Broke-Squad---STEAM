using System;
using Gameplay.Controller;
using Gameplay.Items;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

public class PlayerSetUp : NetworkBehaviour
{
    [Serializable]
    public struct SpawnableObjectInPlayerPrefab
    {
        public GameObject Prefab;
        public Transform Position;
        public bool IsOwner;
    }
    
    [SerializeField] private SpawnableObjectInPlayerPrefab[] _spawnableList;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerInteract _playerInteract;
    [SerializeField] private PlayerInventory _playerInventory;
    [SerializeField] private PlayerInput _playerInput;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && _playerInput != null) 
            _playerInput.enabled = false;
        
        foreach (var s in _spawnableList)
        {
            if (IsOwner == s.IsOwner)
            {
                GameObject g = Instantiate(s.Prefab, s.Position.position, s.Position.rotation, transform);

                if (g.TryGetComponent(out PlayerSetUpBridge setUpBridge))
                {
                    setUpBridge.SetUp(_playerController, _playerHealth, _playerInteract, _playerInventory, _playerInput);
                }
            }
        }
    }
}
