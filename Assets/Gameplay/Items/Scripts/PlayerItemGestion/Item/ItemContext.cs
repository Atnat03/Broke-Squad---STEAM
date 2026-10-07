using Unity.Netcode;
using System.Collections;
using MyPrint;
using UnityEngine;
using FixedString32Bytes = Unity.Collections.FixedString32Bytes;

namespace Gameplay.Items
{
    public class ItemContext
    {
        public ItemCore Core { get; }
        public PlayerInventory Inventory { get; }
        public Camera Camera { get; }
        private readonly PlayerInventoryReplication _replication;

        public ItemContext(ItemCore core, Camera camera, PlayerInventory inventory)
        {
            Core = core;
            Camera = camera;
            Inventory = inventory;
            _replication = inventory.GetComponent<PlayerInventoryReplication>();
        }

        public void SendToServer(IItemModule module, OnModuleDoAction_EVENT itemEvent) => _replication.Send(Core.Instance.IndexOf(module), itemEvent);
        public void SendEventTo(ulong clientId, OnModuleDoAction_EVENT data) => _replication.SendEvent(clientId, data);
        public void SendEventToEveryone(OnModuleDoAction_EVENT data) => _replication.SendEventToEveryOne(data);
        
        public Coroutine StartCoroutine(IEnumerator routine) => _replication != null ? _replication.StartCoroutine(routine) : null;

        public void StopCoroutine(Coroutine routine)
        {
            if (routine != null && _replication != null) _replication.StopCoroutine(routine);
        }
        
        public GameObject InstantiateGameObject(GameObject prefab, Vector3 position, Quaternion rotation) => _replication.InstantiateGameObject(prefab, position, rotation);

        public NetworkObject SpawnGameObject(GameObject prefab, Vector3 position, Quaternion rotation) => _replication.SpawnGameObject(prefab, position, rotation);
        
        public void NotifyStateChanged(IItemModule module)
        {
            if (module is not IReplicatedModule replicated) return;
            _replication.PublishState(Core.Instance.IndexOf(module), replicated.GetState());
        }
    }
}