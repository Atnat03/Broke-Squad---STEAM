using System;
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

        private readonly NetworkList<PhotoChunkState> _photoStates = new();
        private const int PhotoChunkSize = 400;
        
        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            _states.OnListChanged += _ => _inventory.ApplyReplicatedStates();
            _photoStates.OnListChanged += _ => _inventory.ApplyReplicatedStates();
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
            SendSuspiciousSoundEventRpc(clientId, settings.Force, pos);
        
        [Rpc(SendTo.SpecifiedInParams)]
        private void EventRpc(OnModuleDoAction_EVENT data, RpcParams rpcParams = default) => EventBus.InvokeEvent(data);
        
        [Rpc(SendTo.Everyone)]
        private void EventEveryoneRpc(OnModuleDoAction_EVENT data) => EventBus.InvokeEvent(data);

        [Rpc(SendTo.Server)]
        private void SendSuspiciousSoundEventRpc(ulong clientId, float force, Vector3 origin)
        {
            InvokeEvent(new OnCreateSuspiciousSound_EVENT
            {
                FromClientID = clientId,
                Force = force,
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
            
            PublishPhotoStates(item);
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
            
            ApplyPhotoStates(instance);
        }

        #endregion
        
        #region Photo
        
        private void PublishPhotoStates(ItemInstance item)
        {
            _photoStates.Clear();

            if (item == null)
                return;

            int moduleIndex = 0;

            foreach (var module in item.AllModules())
            {
                if (module is IReplicatedByteModule replicated)
                {
                    byte[] bytes = replicated.GetByteState();

                    if (bytes.Length > 0)
                    {
                        string base64 = Convert.ToBase64String(bytes);

                        int chunkCount =
                            Mathf.CeilToInt((float)base64.Length / PhotoChunkSize);

                        for (int i = 0; i < chunkCount; i++)
                        {
                            int start = i * PhotoChunkSize;
                            int length = Mathf.Min(
                                PhotoChunkSize,
                                base64.Length - start
                            );

                            string chunk = base64.Substring(start, length);

                            _photoStates.Add(new PhotoChunkState
                            {
                                ModuleIndex = moduleIndex,
                                ChunkIndex = i,
                                ChunkCount = chunkCount,
                                Data = new FixedString512Bytes(chunk)
                            });
                        }
                    }
                }

                moduleIndex++;
            }
        }
        
        private void ApplyPhotoStates(ItemInstance instance)
        {
            if (instance == null)
                return;

            var groups = new Dictionary<int, List<PhotoChunkState>>();

            foreach (var state in _photoStates)
            {
                if (!groups.TryGetValue(state.ModuleIndex, out var chunks))
                {
                    chunks = new List<PhotoChunkState>();
                    groups.Add(state.ModuleIndex, chunks);
                }

                chunks.Add(state);
            }

            foreach (var pair in groups)
            {
                var chunks = pair.Value;

                chunks.Sort((a, b) => a.ChunkIndex.CompareTo(b.ChunkIndex));

                int expectedCount = chunks[0].ChunkCount;

                // Ne pas appliquer un transfert incomplet.
                if (chunks.Count != expectedCount)
                    continue;

                bool complete = true;
                var builder = new System.Text.StringBuilder();

                for (int i = 0; i < expectedCount; i++)
                {
                    if (chunks[i].ChunkIndex != i ||
                        chunks[i].ChunkCount != expectedCount)
                    {
                        complete = false;
                        break;
                    }

                    builder.Append(chunks[i].Data.ToString());
                }

                if (!complete)
                    continue;

                byte[] bytes;

                try
                {
                    bytes = Convert.FromBase64String(builder.ToString());
                }
                catch (FormatException)
                {
                    continue;
                }

                int index = pair.Key;
                int currentIndex = 0;

                foreach (var module in instance.AllModules())
                {
                    if (currentIndex == index)
                    {
                        if (module is IReplicatedByteModule replicated)
                            replicated.SetByteState(bytes);

                        break;
                    }

                    currentIndex++;
                }
            }
        }
        
        #endregion
    }
}