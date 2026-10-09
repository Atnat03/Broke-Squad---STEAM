using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class LDElementSpawner : MonoBehaviour
    {
        [SerializeField] private LDElement _parentPrefab;
        [SerializeField] private SO_LDElementRegistry _registry;
        [SerializeField] private SO_LDElementData _debugData;

        public void Spawn(SO_LDElementData data, Vector3 position)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[LD] Spawn impossible : aucun NetworkManager dans la scène.", this);
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[LD] Spawn ignoré : seul le serveur peut spawner.", this);
                return;
            }

            if (data == null || _registry == null || _parentPrefab == null)
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
            element.GetComponent<NetworkObject>().Spawn();
        }

        [ContextMenu("Spawn debug element")]
        private void SpawnDebug() => Spawn(_debugData, transform.position);
    }
}