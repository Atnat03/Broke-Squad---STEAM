using Bus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

namespace VFX_Manage
{
    public class VFXManager : MonoBusListener
    {
        [SerializeField] private List<VFXDataSO> vfxData = new();
        [SerializeField] private int maxPerVfx = 20;

        private readonly Dictionary<string, GameObject> prefabs = new();
        private readonly Dictionary<string, Queue<PoolVFX>> free = new();
        private readonly Dictionary<string, int> created = new();

        [Header("Test")]
        [SerializeField] private string nameVFX;
        [SerializeField] private int spamCount = 30;
        [SerializeField] private Vector3 position;


        private void Awake()
        {
            BuildPrefabDictionry();

            ListenToEvent<playVFXEvent>(OnPlayVFX);
        }

        private void BuildPrefabDictionry()
        {
            foreach(VFXDataSO data in vfxData)
            {
                if (data == null) continue;

                foreach(VfxData vfx in data.vfxDataList)
                {
                    if(string.IsNullOrEmpty(vfx.vfxName) || vfx.vfxPrefab == null) continue;

                    string key = data.GetKey(vfx);
                    if (!prefabs.TryAdd(key, vfx.vfxPrefab))
                    {
                        Debug.LogWarning($"[VFXManager]: Duplicate VFX key {key} found in {data.name}. Skipping.");
                    }
                }
    
            }
        }

        private void OnPlayVFX(playVFXEvent e)
        {
            if(!prefabs.TryGetValue(e.vfxName, out GameObject prefab))
            {
                Debug.LogWarning($"[VFXManager]: VFX key {e.vfxName} not found.");
                return;
            }

            PoolVFX vfx = Get(e.vfxName);
            if (vfx == null) return;

            bool noRotation = e.rotation.x == 0f && e.rotation.y == 0f && e.rotation.z == 0f && e.rotation.w == 0f;
            vfx.Play(e.position, noRotation ? Quaternion.identity : e.rotation, e.scale <= 0f ? 1f : e.scale);
        }

        private PoolVFX Get(string vfxName)
        {
            if (!free.TryGetValue(vfxName, out Queue<PoolVFX> queue))
            {
                queue = new Queue<PoolVFX>();
                free[vfxName] = queue;
            }
            if (queue.Count > 0) { return queue.Dequeue();}

            created.TryGetValue(vfxName, out int count);
            if(count >= maxPerVfx) { return null; }

            created[vfxName] = count + 1;

            GameObject go = Instantiate(prefabs[vfxName], transform);
            go.SetActive(false);

            PoolVFX poolVFX = go.AddComponent<PoolVFX>();
            if(poolVFX == null) poolVFX = go.AddComponent<PoolVFX>();
            poolVFX.Init(vfxName, Release);
            return poolVFX;
        }

        private void Release(PoolVFX vfx) => free[vfx.Key].Enqueue(vfx);

        #region Test

        [ContextMenu("Test Event Bus VFX")]
        private void TestViaEvent()
        {
            Bus.EventBus.InvokeEvent(new playVFXEvent
            {
                vfxName = nameVFX,
                position = position,
                shared = false
            });
        }

        [ContextMenu("Test Event Direct VFX")]
        private void TestSansEvent()
        {
            OnPlayVFX(new playVFXEvent
            {
                vfxName = nameVFX,
                position = position,
                shared = false
            });
        }

        [ContextMenu("Test rafale pool")]
        private void TestSpam()
        {
            for (int i = 0; i < spamCount; i++)
            {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 2f;
                Bus.EventBus.InvokeEvent(new playVFXEvent
                {
                    vfxName = nameVFX,
                    position = position,
                    scale = UnityEngine.Random.Range(0.8f, 1.2f)
                });
            }
            LogPoolState();
        }

        [ContextMenu("Log keys")]
        private void LogKeys()
        {
            Debug.Log($"VfxManager clés ({prefabs.Count}) : {string.Join(" | ", prefabs.Keys)}");
        }

        [ContextMenu("Log pool")]
        private void LogPoolState()
        {
            foreach (var pair in created)
            {
                free.TryGetValue(pair.Key, out var queue);
                Debug.Log($"Pool '{pair.Key}' : {pair.Value} créés, {(queue?.Count ?? 0)} libres");
            }
        }

        #endregion
    }
}
