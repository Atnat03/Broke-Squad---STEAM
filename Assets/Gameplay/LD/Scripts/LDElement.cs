using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class LDElement : NetworkBehaviour
    {
        [SerializeField] private LDFlag _initialFlags;
        [SerializeField] private List<LDBinding> _bindings = new();

        private readonly NetworkVariable<int> _stateFlags = new(0);
        private LDElementModule[] _modules;
        private LDElementContext _context;

        public event Action<LDFlag> FlagsChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _context = new LDElementContext(this);
            _modules = GetComponentsInChildren<LDElementModule>(true);
            foreach (var module in _modules)
                module.Initialize(_context);

            if (IsServer)
            {
                _stateFlags.Value = (int)_initialFlags;

                for (int i = 0; i < _bindings.Count; i++)
                {
                    int index = i;
                    var trigger = _bindings[i].Trigger;
                    if (trigger == null)
                    {
                        Debug.LogWarning($"[LD] {name} : binding {i} sans trigger", this);
                        continue;
                    }
                    trigger.Triggered += activation => OnTriggered(index, activation);
                }
            }

            _stateFlags.OnValueChanged += OnStateFlagsChanged;
        }

        public override void OnNetworkDespawn()
        {
            _stateFlags.OnValueChanged -= OnStateFlagsChanged;
            base.OnNetworkDespawn();
        }

        private void OnTriggered(int bindingIndex, LDActivation activation)
        {
            var binding = _bindings[bindingIndex];
            if (!ConditionsPass(binding, activation)) return;

            for (int e = 0; e < binding.Effects.Length; e++)
            {
                var effect = binding.Effects[e];
                if (effect == null) continue;

                if (effect.RunsOn == LDEffectModule.EffectTarget.Server)
                    effect.Activate(activation);
                else
                    ActivateEffectClientsRpc(bindingIndex, e, activation);
            }
        }

        private bool ConditionsPass(LDBinding binding, LDActivation activation)
        {
            foreach (var condition in binding.Conditions)
            {
                if (condition == null) continue;
                if (!condition.CanActivate(activation)) return false;
            }
            return true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ActivateEffectClientsRpc(int bindingIndex, int effectIndex, LDActivation activation)
        {
            var effect = _bindings[bindingIndex].Effects[effectIndex];
            if (effect != null) effect.Activate(activation);
        }

        #region flags
        public bool HasFlag(LDFlag flag) => (_stateFlags.Value & (int)flag) != 0;

        public void SetFlag(LDFlag flag, bool value)
        {
            if (!IsServer) return;
            int v = _stateFlags.Value;
            _stateFlags.Value = value ? v | (int)flag : v & ~(int)flag;
        }

        private void OnStateFlagsChanged(int previous, int current)
            => FlagsChanged?.Invoke((LDFlag)current);
        #endregion

        #region interact
        internal void RequestInteract(LDElementModule module)
        {
            int index = Array.IndexOf(_modules, module);
            if (index >= 0) InteractServerRpc(index);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void InteractServerRpc(int moduleIndex, RpcParams rpcParams = default)
        {
            if (moduleIndex < 0 || moduleIndex >= _modules.Length) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;
            if (client.PlayerObject == null) return;
            if (_modules[moduleIndex] is InteractTrigger trigger)
                trigger.ServerInteract(client.PlayerObject);
        }
        #endregion
    }
}