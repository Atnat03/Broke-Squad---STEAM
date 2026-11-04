using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class SpawnItemsEffect : LDEffectModule
    {
        [SerializeField] private NetworkObject _itemPrefab;
        [SerializeField] private Transform[] _pickupSpawnPoints;
        [SerializeField] private float _explosionForce = 300f;
        [SerializeField, SoundName] private string _breakSound;

        public override void Activate(LDActivation activation)
        {
            if (_itemPrefab == null) return;

            foreach (var point in _pickupSpawnPoints)
            {
                NetworkObject item = Instantiate(_itemPrefab, point.position, point.rotation);
                item.Spawn();
                float radius = Context.Element.transform.localScale.magnitude;
                if (item.TryGetComponent(out Rigidbody rb))
                { 
                    rb.AddExplosionForce(_explosionForce, transform.position, radius);
                    InvokeEvent(new PlaySoundEvent
                    {
                        position = transform.position,
                        soundName = _breakSound,
                        volume = 0.3f
                    });
                }
            }
        }
    }
}
