using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class LDElementSpawner : NetworkBehaviour
    {
        [SerializeField] private LDElement _parentPrefab;
        [SerializeField] private SO_LDElementData _debugData;

        public override void OnNetworkSpawn()
        {
            Spawn(_debugData, transform.position);
        }
        
        private void Spawn(SO_LDElementData data, Vector3 position)
        {
            /*if (data == null || _registry == null || _parentPrefab == null)
            {
                Debug.LogError("[LD] Spawn impossible : data, registry ou prefab parent non assigné.", this);
                return;
            }

            int index = _registry.IndexOf(data);
            if (index < 0)
            {
                Debug.LogError($"[LD] {data.name} n'est pas dans le LDElementRegistry.", this);
                return;
            }

            LDElement element = Instantiate(_parentPrefab, position, Quaternion.identity);
            element.PrepareOnServer(index);
            
            */
        }
    }
}