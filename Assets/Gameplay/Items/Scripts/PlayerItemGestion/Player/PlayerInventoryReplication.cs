using System;
using System.Collections;
using System.Collections.Generic;
using Bus;
using Gameplay.Other.SuspiciousSound;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInventoryReplication : NetworkBusListener
    {
        private PlayerInventory _inventory;
        private readonly NetworkList<ItemModuleState> _states = new NetworkList<ItemModuleState>();
        private readonly List<ItemModuleState> _buffer = new();

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            _states.OnListChanged += _ => _inventory.ApplyReplicatedStates();
        }

        #region Rpc Server

        public void Send(int moduleIndex, OnModuleDoAction_EVENT itemEvent) => ActionRpc(moduleIndex, itemEvent);

        [Rpc(SendTo.Server)]
        private void ActionRpc(int moduleIndex, OnModuleDoAction_EVENT itemEvent)
        {
            ItemInstance item = _inventory.GetCurrentItemInHand();
            if (item == null) return;

            if (item.GetModule(moduleIndex) is IServerAction action)
                action.ServerExecute(itemEvent);
        }

        #endregion
        
        #region Send Events

        public void SendEvent(ulong clientId, OnModuleDoAction_EVENT data) => EventRpc(data, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        public void SendEventToEveryOne(OnModuleDoAction_EVENT data) => EventEveryoneRpc(data);

        public void SendSuspiciousSoundEvent(ulong clientId, Vector3 pos, SO_SuspiciousSoundSettings settings) => 
            SendSuspiciousSoundEventRpc(clientId, settings.Force, settings.MaxDistance, pos);
        
        [Rpc(SendTo.SpecifiedInParams)]
        private void EventRpc(OnModuleDoAction_EVENT data, RpcParams rpcParams = default) => EventBus.InvokeEvent(data);
        
        [Rpc(SendTo.Everyone)]
        private void EventEveryoneRpc(OnModuleDoAction_EVENT data) => EventBus.InvokeEvent(data);

        [Rpc(SendTo.Server)]
        private void SendSuspiciousSoundEventRpc(ulong clientId, float force, float distance, Vector3 origin)
        {
            InvokeEvent(new OnCreateSuspiciousSound_EVENT
            {
                FromClientID = clientId,
                Force = force,
                MaxDistance = distance,
                Position = origin
            });
        }

        #endregion

        #region Instantiate
        
        public GameObject InstantiateGameObject(GameObject prefab, Vector3 position, Quaternion rotation) => Instantiate(prefab, position, rotation);

        public NetworkObject SpawnGameObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            NetworkObject net = Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
            net.Spawn();
            return net;
        }
        
        #endregion
        
        #region Module States

        public void PublishHeldItem(ItemInstance item)
        {
            if (!IsServer) return;

            _states.Clear();
            if (item == null) return;

            _buffer.Clear();
            item.CollectStates(_buffer);
            for (int i = 0; i < _buffer.Count; i++)
                _states.Add(_buffer[i]);
        }

        public void PublishState(int moduleIndex, int value)
        {
            if (!IsServer) return;

            var state = new ItemModuleState { ModuleIndex = moduleIndex, Value = value };
            for (int i = 0; i < _states.Count; i++)
            {
                if (_states[i].ModuleIndex != moduleIndex) continue;
                _states[i] = state;
                return;
            }
            _states.Add(state);
        }

        public void ApplyTo(ItemInstance instance)
        {
            if (instance == null) return;
            for (int i = 0; i < _states.Count; i++)
                instance.ApplyState(_states[i]);
        }

        #endregion
    }
}