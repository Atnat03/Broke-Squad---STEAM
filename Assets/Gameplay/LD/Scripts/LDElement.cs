using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.STP;

namespace Gameplay.LD
{
    public class LDElement : NetworkBehaviour
    {
        [SerializeField] private LDElementRegistry _registry;
        private readonly NetworkVariable<int> _dataIndex = new NetworkVariable<int>(-1);
        private LDElementData _data;
        private LDElementContext _context;
        private readonly Dictionary<int, LDElementModule> _modulesById = new ();

        public void PrepareOnServer(int registryIndex)
        {
            _dataIndex.Value = registryIndex;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _data = _registry.Get(_dataIndex.Value);
            if(_data == null)
            {
                Debug.LogError($"LDElement {name} has invalid data index {_dataIndex.Value}");
                return;
            }

            BuildFromData();
        }

        private void BuildFromData()
        {
            _context = new LDElementContext(this);

            foreach (ModuleConfig config in _data.ModulesList)
            {
                GameObject child = new GameObject(config.ModuleType.Name);
                child.transform.SetParent(transform, false);
                LDElementModule module = (LDElementModule)child.AddComponent(config.ModuleType);
                module.Initialize(_context, config);
                _modulesById[config.ModuleID] = module;
            }
        }
    }
}
