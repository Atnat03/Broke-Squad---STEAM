using Bus;
using Gameplay.IA;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.LD.Proto1
{
    public class Turret : NetworkBusListener
    { 
        [Header("State")]
        [SerializeField] private bool startActive = false;
    
       [Header("Detection")]
       [SerializeField] private float detectionRange = 7f;
       [SerializeField] private bool requireLineOfSight = true;
       [SerializeField] private LayerMask obstacleMask = ~0;   
    
       [Header("Shooting")]
       [SerializeField] private TurretProjectile projectilePrefab; 
       [SerializeField] private Transform muzzle;
       [SerializeField] private float fireInterval = 1.5f;
       [SerializeField] private float aimOffsetY = 1f;        
    
       [Header("Visuals")]
       [SerializeField] private Transform turretHead;          
       [SerializeField] private float rotationSpeed = 8f;
       [SerializeField] private LineRenderer lineRenderer;
       
       [Header("Inactive pose")]
       [SerializeField] private float inactivePitch = 60f;  
       [SerializeField] private float inactiveRotationSpeed = 3f;
       
       private readonly NetworkVariable<bool> isActive = new NetworkVariable<bool>(true);
       private readonly NetworkVariable<ulong> targetId = new NetworkVariable<ulong>(ulong.MaxValue);
    
       private float nextFireTime;
    
       public bool IsActive => isActive.Value;
    
       public override void OnNetworkSpawn()
       {
           if (IsServer) isActive.Value = startActive;
    
           isActive.OnValueChanged += (_, __) => RefreshVisuals();
           //ListenToEvent<OnLockDownStart_EVENT>(_=>SetActive(true));
           SetActive(startActive);
           RefreshVisuals();
       }

       public override void OnNetworkDespawn()
       {
           base.OnNetworkDespawn();
       }

       public void SetActive(bool value)
       {
           if (!IsServer) return; 
           isActive.Value = value;
           if (!value) targetId.Value = ulong.MaxValue;
       }
       
       
       private void Update()
       {
           if (IsServer && isActive.Value) ServerLogic();
           if(isActive.Value)
           {
               //lineRenderer.enabled = true;
               //lineRenderer.SetPosition(0, turretHead.position);
               //lineRenderer.SetPosition(1, muzzle.position);
               AimVisual();
           }
       }
    
       private void ServerLogic()
       {
           NetworkObject target = FindClosestTarget();
           targetId.Value = target != null ? target.NetworkObjectId : ulong.MaxValue;
    
           if (target == null || Time.time < nextFireTime) return;
    
           nextFireTime = Time.time + fireInterval;
           Fire(target.transform.position + Vector3.up * aimOffsetY);
       }
    
       private NetworkObject FindClosestTarget()
       {
           NetworkObject best = null;
           float bestDist = detectionRange;
           Vector3 origin = muzzle.position;
    
           foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
           {
               var player = client.PlayerObject;
               if (player == null) continue;
    
               Vector3 pos = player.transform.position + Vector3.up * aimOffsetY;
               float d = Vector3.Distance(origin, pos);
               if (d > bestDist) continue;
    
               if (requireLineOfSight &&
                   Physics.Linecast(origin, pos, obstacleMask, QueryTriggerInteraction.Ignore))
                   continue;
    
               best = player;
               bestDist = d;
           }
           return best;
       }
    
       private void Fire(Vector3 aimPoint)
       {
           Vector3 dir = (aimPoint - muzzle.position).normalized;
           
           var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
           proj.GetComponent<NetworkObject>().Spawn(true);
           proj.Init(dir);
       }
       
       private void AimVisual()
       {
           if (turretHead == null || targetId.Value == ulong.MaxValue) return;
    
           if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                   .TryGetValue(targetId.Value, out NetworkObject target))
           {
               Vector3 dir = target.transform.position - turretHead.position;
               if (dir.sqrMagnitude < 0.001f) return;
    
               turretHead.rotation = Quaternion.Slerp(
                   turretHead.rotation,
                   Quaternion.LookRotation(dir),
                   rotationSpeed * Time.deltaTime);
           }
       }
    
       private void RefreshVisuals()
       {
           if (!isActive.Value) turretHead.rotation = Quaternion.LookRotation(muzzle.position - turretHead.position);
       }
    
       private void OnDrawGizmosSelected()
       {
           Gizmos.color = Color.red;
           Gizmos.DrawWireSphere(transform.position, detectionRange);
       }
    }
}