using System;
using MyPrint;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardLineOfSight : NetworkBehaviour
    {
        // ---------- Joueur ----------
        public bool CanSeeTarget { get; private set; }

        public Transform Target{ get; private set; }
        /*{
            get => _visibleTargetPlayer;
            private set => GuardBrain.PlayerTarget = _visibleTargetPlayer;
        }*/
        
        public float DetectionProgress => _detectionLevel > 0f ? Mathf.Clamp01(_detectionTimer.Value / _detectionLevel) : 1f;

        public event Action<Transform> OnTargetSeen;
        public event Action OnGetNewTarget;
        public event Action OnTargetLost;
        public event Action<bool> OnNotifyTargetSeen;
        public event Action<float> OnDetectionProgressChanged;
        public event Action<Transform> OnDetectionCompleted;

        // ---------- Suspicieux ----------
        public bool CanSeeSuspicious { get; private set; }

        public Transform SuspiciousTarget{ get; private set; }
        /*{
            get => _visibleTargetSuspicious;
            private set => GuardBrain.SuspectTarget = _visibleTargetSuspicious;
        }*/
        public float SuspiciousProgress => _suspiciousLevel > 0f ? Mathf.Clamp01(_suspiciousTimer.Value / _suspiciousLevel) : 1f;

        public event Action<Transform> OnSuspiciousSeen;
        public event Action OnGetNewSuspicious;
        public event Action OnSuspiciousLost;
        public event Action<bool> OnNotifySuspectTargetSeen;
        public event Action<float> OnSuspiciousProgressChanged;

        [Header("Vision")]
        [SerializeField] private float _radius = 10f;
        [SerializeField, Range(0f, 360f)] private float _angle = 90f;
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _checkInterval = 0.2f;

        [Header("Detection Player")]
        [SerializeField] private float _detectionLevel = 2f;
        [SerializeField] private float _detectionSpeed = 2f;
        [SerializeField] private float _forgettingSpeed = 1f;

        [Header("Detection Suspicious")]
        [SerializeField] private float _suspiciousLevel = 2f;
        [SerializeField] private float _suspiciousSpeed = 2f;
        [SerializeField] private float _suspiciousForgettingSpeed = 1f;

        [Header("Layers")]
        [SerializeField] private LayerMask _targetPlayerMask;
        [SerializeField] private LayerMask _targetSuspiciousMask;
        [SerializeField] private LayerMask _obstructionMask;

        private readonly Collider[] _bufferPlayer = new Collider[8];
        private readonly Collider[] _bufferSuspicious = new Collider[8];

        private float _nextCheck;
        private readonly NetworkVariable<float> _detectionTimer = new();
        private NetworkVariable<float> _suspiciousTimer = new();
        private Transform _visibleTargetPlayer;
        private Transform _visibleTargetSuspicious;

        public float Radius => _radius;
        public float Angle => _angle;
        public Vector3 EyePosition => transform.position + Vector3.up * _eyeHeight;

        public GuardBehavior GuardBrain;

        public override void OnNetworkSpawn()
        {
            _detectionTimer.OnValueChanged += OnDetectionTimerChange;
            _suspiciousTimer.OnValueChanged += OnSuspiciousTimerChange;
        }

        public override void OnNetworkDespawn()
        {
            _detectionTimer.OnValueChanged -= OnDetectionTimerChange;
            _suspiciousTimer.OnValueChanged -= OnSuspiciousTimerChange;
        }

        private void Update()
        {
            if (!IsServer)
                return;
            
            // La vision est testée à intervalle régulier,
            // mais les timers sont mis à jour à chaque frame.
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + _checkInterval;
                _visibleTargetPlayer = FindVisible(_targetPlayerMask, _bufferPlayer);
                _visibleTargetSuspicious = FindVisible(_targetSuspiciousMask, _bufferSuspicious);
            }

            UpdatePlayerDetection();
            UpdateSuspiciousDetection();
        }

        private void UpdatePlayerDetection()
        {
            CanSeeTarget = _visibleTargetPlayer != null;
            
            if (CanSeeTarget)
            {
                // Détection : le timer monte jusqu'au seuil.
                _detectionTimer.Value = Mathf.Min(_detectionTimer.Value + Time.deltaTime * _detectionSpeed, _detectionLevel);

                if (Target == null)
                {
                    // La cible n'est acquise qu'une fois le seuil atteint.
                    if (_detectionTimer.Value >= _detectionLevel)
                    {
                        Target = _visibleTargetPlayer;
                        
                        OnGetNewTarget?.Invoke();
                        OnTargetSeen?.Invoke(Target);
                        OnDetectionCompleted?.Invoke(Target);
                        OnNotifyTargetStateRpc(true);
                    }
                }
                else
                {
                    // Garder la cible à jour tant qu'elle reste visible.
                    Target = _visibleTargetPlayer;
                }
            }
            else
            {
                // Oubli : le timer descend jusqu'à 0.
                _detectionTimer.Value = Mathf.Max(_detectionTimer.Value - Time.deltaTime * _forgettingSpeed, 0f);

                // La cible n'est perdue qu'une fois le timer vidé.
                if (_detectionTimer.Value <= 0f && Target != null)
                {
                    Target = null;
                    OnTargetLost?.Invoke();
                    OnNotifyTargetStateRpc(false);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void OnNotifyTargetStateRpc(bool state) => OnNotifyTargetSeen?.Invoke(state);

        private void UpdateSuspiciousDetection()
        {
            CanSeeSuspicious = _visibleTargetSuspicious != null;
            
            if (CanSeeSuspicious)
            {
                // Détection : le timer monte jusqu'au seuil.
                _suspiciousTimer.Value = Mathf.Min(_suspiciousTimer.Value + Time.deltaTime * _suspiciousSpeed, _suspiciousLevel);

                if (SuspiciousTarget == null)
                {
                    // L'objet n'est acquis qu'une fois le seuil atteint.
                    if (_suspiciousTimer.Value >= _suspiciousLevel)
                    {
                        SuspiciousTarget = _visibleTargetSuspicious;

                        OnGetNewSuspicious?.Invoke();
                        OnSuspiciousSeen?.Invoke(SuspiciousTarget);
                        OnNotifySuspiciousTargetStateRpc(true);
                    }
                }
                else
                {
                    // Garder l'objet à jour tant qu'il reste visible.
                    SuspiciousTarget = _visibleTargetSuspicious;
                }
            }
            else
            {
                // Oubli : le timer descend jusqu'à 0.
                _suspiciousTimer.Value = Mathf.Max(_suspiciousTimer.Value - Time.deltaTime * _suspiciousForgettingSpeed, 0f);

                // L'objet n'est oublié qu'une fois le timer vidé.
                if (_suspiciousTimer.Value <= 0f && SuspiciousTarget != null)
                {
                    SuspiciousTarget = null;
                    OnSuspiciousLost?.Invoke();
                    OnNotifySuspiciousTargetStateRpc(false);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void OnNotifySuspiciousTargetStateRpc(bool state) => OnNotifySuspectTargetSeen?.Invoke(state);

        
        private Transform FindVisible(LayerMask mask, Collider[] buffer)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, buffer, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform target = buffer[i].transform;
                Vector3 targetPoint = buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - EyePosition;

                Vector3 flatDir = new Vector3(dirToTarget.x, 0f, dirToTarget.z);

                if (flatDir.sqrMagnitude < 0.0001f) return target;

                if (Vector3.Angle(transform.forward, flatDir) > _angle * 0.5f) continue;

                if (Physics.Linecast(EyePosition, targetPoint, _obstructionMask, QueryTriggerInteraction.Ignore)) continue;

                return target;
            }

            return null;
        }
        
        private void OnDetectionTimerChange(float previousValue, float newValue)
        {
            if (Mathf.Abs(previousValue - newValue) > 0.001f)
            {
                OnDetectionProgressChanged?.Invoke(_detectionLevel > 0f ? Mathf.Clamp01(newValue / _detectionLevel) : 1f);
            }
        }
        
        private void OnSuspiciousTimerChange(float previousValue, float newValue)
        {
            if(Math.Abs(previousValue - newValue) < 0.01f)
                OnSuspiciousProgressChanged?.Invoke(newValue / _suspiciousLevel);
        }
        
        public void ForceTarget(Transform newTarget)
        {
            if (newTarget == null)
            {
                if (Target != null)
                {
                    Target = null;
                    _detectionTimer.Value = 0f; 
                    OnTargetLost?.Invoke();
                    OnNotifyTargetStateRpc(false);
                }
                return;
            }

            if (Target == newTarget) return;


            Target = newTarget;
            _detectionTimer.Value = _detectionLevel;

            OnGetNewTarget?.Invoke();
            OnTargetSeen?.Invoke(Target);
            OnDetectionCompleted?.Invoke(Target);
            OnNotifyTargetStateRpc(true);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;

            Gizmos.color = Color.white;
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawWireDisc(pos, Vector3.up, _radius);

            Vector3 left = Quaternion.Euler(0, -_angle * 0.5f, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0, _angle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + left * _radius);
            Gizmos.DrawLine(pos, pos + right * _radius);
        }
#endif
    }
}
