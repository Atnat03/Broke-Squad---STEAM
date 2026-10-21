using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Proto1
{
    public class TurretProjectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float hitRadius = 0.5f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private LayerMask obstacleLayer = ~0;
        
        private Vector3 direction;
        private float despawnTime;
        private bool initialized;
        
        public void Init(Vector3 dir)
        {
            direction = dir.normalized;
            despawnTime = Time.time + lifeTime;
            initialized = true;
        }
 
        private void Update()
        {
            if (!IsServer || !initialized) return;
 
            float step = speed * Time.deltaTime;
            Vector3 from = transform.position;
            Vector3 to = from + direction * step;
            
            if (Physics.Linecast(from, to, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                Despawn();
                return;
            }
 
            transform.position = to;
            
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var player = client.PlayerObject;
                if (player == null) continue;
 
                var col = player.GetComponentInChildren<Collider>();
                Vector3 center = col != null ? col.bounds.center : player.transform.position;
                if (Vector3.Distance(to, center) <= hitRadius + 0.5f)
                {
                    if (player.TryGetComponent<IDamageable>(out var target))
                        target.ApplyDamage(damage);
 
                    Despawn();
                    return;
                }
            }
 
            if (Time.time >= despawnTime) Despawn();
        }
 
        private void Despawn()
        {
            initialized = false;
            NetworkObject.Despawn(true);
        }
    }
    
}