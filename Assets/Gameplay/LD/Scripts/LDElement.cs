using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class LDElement : NetworkBehaviour
    {
        [SerializeField] private SO_LDElementRegistry _registry;
        [SerializeField] private SO_LDElementData _data;

        private readonly NetworkVariable<int> _dataIndex = new NetworkVariable<int>(-1);
        private readonly NetworkVariable<int> _stateFlags = new(0);
        
        private LDElementContext _context;
        private readonly Dictionary<int, LDElementModule> _modulesById = new ();
        public event Action<LDFlag> FlagsChanged;
        
        public void PrepareOnServer(int registryIndex)
        {
            Debug.Log("[LD] PrepareOnServer: " + registryIndex);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if(IsServer)
                _dataIndex.Value = _registry.IndexOf(_data);

            Debug.Log($"[LD] OnNetworkSpawn: {name} index {_dataIndex.Value}");

            if (_registry == null)
            {
                Debug.LogError($"[LD] {name} has no registry assigned");
                return;
            }

            if(_data == null)
            {
                Debug.LogError($"[LD] {name} has invalid data index {_dataIndex.Value}");
                return;
            }

            if(IsServer) 
            {
                _stateFlags.Value = (int)_data.initialFlags;
                Debug.Log($"[LD] client flags: {_stateFlags.Value}");
            }
            _stateFlags.OnValueChanged += OnStateFlagsChanged;

            BuildFromData();
        }

        public override void OnNetworkDespawn()
        {
            _stateFlags.OnValueChanged -= OnStateFlagsChanged;
            base.OnNetworkDespawn();
        }

        private void BuildFromData()
        {
            if(_data.Prefab != null)
            {
                Instantiate(_data.Prefab, transform);
            }

            _context = new LDElementContext(this);

            foreach (ModuleConfig config in _data.ModulesList)
            {
                if (config == null) continue;
                GameObject child = new GameObject(config.ModuleType.Name);
                child.transform.SetParent(transform, false);
                LDElementModule module = (LDElementModule)child.AddComponent(config.ModuleType);
                module.Initialize(_context, config);
                _modulesById[config.ModuleID] = module;
            }

            for (int i = 0; i < _data.BindingsList.Count; i++)
            {
                if(!IsServer) continue;
                int index = i;
                var binding = _data.BindingsList[i];
                if (!TryGetModule<LDTriggerModule>(binding.TriggerID, out var trigger)) continue;
                trigger.Triggered += activation => OnTriggered(index, activation);
            }
        }

        private void OnTriggered(int index, LDActivation activation)
        {
            Debug.Log("[LD] OnTriggered");

            LDBindingData binding = _data.BindingsList[index];
            if(!ConditionsPass(binding, activation)) return;

            Debug.Log("[LD] conditions ok");

            foreach (int id in _data.BindingsList[index].EffectIDs)
            {
                if (!TryGetModule<LDEffectModule>(id, out var effect)) continue;

                if (effect.RunsOn == LDEffectModule.EffectTarget.Server)
                    effect.Activate(activation);
                else
                    ActivateEffectClientsRPC(id, activation);
            }
        }

        public bool TryGetModule<T>(int id, out T module) where T : LDElementModule
        {
            module = null;
            if (!_modulesById.TryGetValue(id, out var m)) { Debug.LogError($"Module {id} introuvable", this); return false; }
            module = m as T;
            if (module == null) Debug.LogError($"Module {id} n'est pas un {typeof(T).Name}", this);
            return module != null;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ActivateEffectClientsRPC(int moduleID, LDActivation activation)
        {
            if (!TryGetModule<LDEffectModule>(moduleID, out var effect)) return;
            effect.Activate(activation);
        }

        #region flags

        public bool HasFlag(LDFlag flag) => (_stateFlags.Value & (int)flag) != 0;

        public void SetFlag(LDFlag flag, bool value)
        {
            if (!IsServer) return;
            int v = _stateFlags.Value;
            _stateFlags.Value = value ? v | (int)flag : v & ~(int)flag;
        }

        private void OnStateFlagsChanged(int previous, int current) => FlagsChanged?.Invoke((LDFlag) current);
        #endregion

        private bool ConditionsPass(LDBindingData binding, LDActivation activation)
        {
            foreach (int conditionID in binding.ConditionIDs)
            {
                if (!TryGetModule<LDConditionModule>(conditionID, out var condition)) continue;
                if (!condition.CanActivate(activation)) return false;
            }
            return true;
        }

        internal void RequestInteract(int moduleId) => InteractServerRpc(moduleId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void InteractServerRpc(int moduleId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;
            if (client.PlayerObject == null) return;
            if (!TryGetModule<InteractTrigger>(moduleId, out var trigger)) return;
            trigger.ServerInteract(client.PlayerObject);
        }
    }
}