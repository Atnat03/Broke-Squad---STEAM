using System;
using System.Collections;
using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInventoryReplication : NetworkBusListener
    {
        private PlayerInventory _inventory;

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
        }

        public void Send(int moduleIndex, OnModuleDoAction_EVENT itemEvent) => ActionRpc(moduleIndex, itemEvent);

        [Rpc(SendTo.Server)]
        private void ActionRpc(int moduleIndex, OnModuleDoAction_EVENT itemEvent)
        {
            ItemInstance item = _inventory.GetCurrentItemInHand();
            if (item == null) return;

            if (item.GetModule(moduleIndex) is IServerAction action)
                action.ServerExecute(itemEvent);
        }
        
        public void SendEvent(ulong clientId, OnModuleDoAction_EVENT data) => EventRpc(data, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        public void SendEventToEveryOne(OnModuleDoAction_EVENT data) => EventEveryoneRpc(data);

        [Rpc(SendTo.SpecifiedInParams)]
        private void EventRpc(OnModuleDoAction_EVENT data, RpcParams rpcParams = default) => EventBus.InvokeEvent(data);
        
        [Rpc(SendTo.Everyone)]
        private void EventEveryoneRpc(OnModuleDoAction_EVENT data) => EventBus.InvokeEvent(data);

        public GameObject InstantiateGameObject(GameObject prefab, Vector3 position, Quaternion rotation) => Instantiate(prefab, position, rotation);

        public NetworkObject SpawnGameObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            NetworkObject net = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
            net.Spawn();
            return net;
        }
    }
}