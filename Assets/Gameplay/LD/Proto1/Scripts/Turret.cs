using Bus;
using Gameplay.IA;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.LD.Proto1
{
    public class Turret : NetworkBusListener
    {
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

        [Header("Laser (only visible when active)")]
        [SerializeField] private LineRenderer lineRenderer;  
        [SerializeField] private Color idleLaserColor = new Color(1f, 0.8f, 0f, 0.6f);
        [SerializeField] private Color lockedLaserColor = new Color(1f, 0.1f, 0.1f, 1f);

        [Header("Inactive pose")]
        [SerializeField] private float inactivePitch = 60f;
        [SerializeField] private float inactiveRotationSpeed = 3f;

        private readonly NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
        private readonly NetworkVariable<ulong> targetId = new NetworkVariable<ulong>(ulong.MaxValue);

        private float nextFireTime;
        private Vector3 restForward;

        public bool IsActive => isActive.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            restForward = turretHead.forward;
            restForward.y = 0f;
            if (restForward.sqrMagnitude < 0.001f) restForward = Vector3.forward;
            restForward.Normalize();

            isActive.OnValueChanged += OnActiveChanged;
            ListenToEvent<OnLockDownStart_EVENT>(_=>SetActive(true));
            ListenToEvent<OnLockDownEnd_EVENT>(_=>SetActive(false));
            RefreshVisuals();
        }

        public override void OnNetworkDespawn()
        {
            isActive.OnValueChanged -= OnActiveChanged;
            base.OnNetworkDespawn();
        }

        private void OnActiveChanged(bool previous, bool current) => RefreshVisuals();

        public void SetActive(bool value)
        {
            if (!IsServer) return;
            isActive.Value = value;
            if (!value) targetId.Value = ulong.MaxValue;
            else nextFireTime = Time.time + 0.5f; 
        }

        private void Update()
        {
            if (IsServer && isActive.Value) ServerLogic();

            AimVisual();  
            UpdateLaser();
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

        
        private bool TryGetTarget(out NetworkObject target)
        {
            target = null;
            return targetId.Value != ulong.MaxValue &&
                   NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.SpawnManager.SpawnedObjects
                       .TryGetValue(targetId.Value, out target);
        }

        private void AimVisual()
        {
            if (turretHead == null) return;

            Quaternion desired;
            float speed;

            if (!isActive.Value)
            {
                Vector3 right = Vector3.Cross(Vector3.up, restForward);
                Vector3 downDir = Quaternion.AngleAxis(inactivePitch, right) * restForward;
                desired = Quaternion.LookRotation(downDir);
                speed = inactiveRotationSpeed;
            }
            else if (TryGetTarget(out NetworkObject target))
            {
                Vector3 dir = (target.transform.position + Vector3.up * aimOffsetY) - turretHead.position;
                if (dir.sqrMagnitude < 0.001f) return;
                desired = Quaternion.LookRotation(dir);
                speed = rotationSpeed;
            }
            else
            {
                desired = Quaternion.LookRotation(restForward);
                speed = rotationSpeed;
            }

            turretHead.rotation = Quaternion.Slerp(turretHead.rotation, desired, speed * Time.deltaTime);
        }

        private void UpdateLaser()
        {
            if (lineRenderer == null) return;

            lineRenderer.enabled = isActive.Value;
            if (!isActive.Value) return;

            Vector3 start = muzzle.position;
            Vector3 end;
            Color color;

            if (TryGetTarget(out NetworkObject target))
            {
                end = target.transform.position + Vector3.up * aimOffsetY;
                color = lockedLaserColor;
            }
            else
            {
                end = start + muzzle.forward * detectionRange;
                if (Physics.Raycast(start, muzzle.forward, out RaycastHit hit, detectionRange,
                        obstacleMask, QueryTriggerInteraction.Ignore))
                    end = hit.point;
                color = idleLaserColor;
            }

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        private void RefreshVisuals()
        {
            if (lineRenderer != null) lineRenderer.enabled = isActive.Value;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.DrawLine(muzzle.position, muzzle.position);
        }
    }
}