using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.IA.Scripts
{
    public class GuardLineOfSight : MonoBehaviour
    {
        public bool CanSeeTarget { get; private set; }
        public Transform Target { get; private set; }

        public float DetectionProgress => _detectionLevel > 0f ? Mathf.Clamp01(_detectionTimer / _detectionLevel) : 1f;

        public event Action<Transform> OnTargetSeen;
        public event Action OnTargetLost;
        public event Action<float> OnDetectionProgressChanged;
        public event Action<Transform> OnDetectionCompleted;

        [Header("Vision")]
        [SerializeField] private float _radius = 10f;
        [SerializeField, Range(0f, 360f)] private float _angle = 90f;
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _checkInterval = 0.2f;

        [Header("Detection")]
        [SerializeField] private float _detectionLevel = 2f;
        [SerializeField] private float _detectionSpeed = 2f;
        [SerializeField] private float _forgettingSpeed = 1f;

        [Header("Layers")]
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LayerMask _obstructionMask;

        private readonly Collider[] _buffer = new Collider[8];

        private float _nextCheck;
        private float _detectionTimer;
        private Transform _visibleTarget;

        public float Radius => _radius;
        public float Angle => _angle;
        public Vector3 EyePosition => transform.position + Vector3.up * _eyeHeight;

        private void Update()
        {
            // La vision est testée à intervalle régulier,
            // mais les timers sont mis à jour à chaque frame.
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + _checkInterval;
                _visibleTarget = FindVisibleTarget();
            }
            
            if(Target)
                ABPrint.Print(Target.ToString(), ABColor.Pink);
            ABPrint.Print("Detection : " + _detectionTimer, ABColor.Purple);

            CanSeeTarget = _visibleTarget != null;

            float previousTimer = _detectionTimer;

            if (CanSeeTarget)
            {
                // Détection : le timer monte jusqu'au seuil.
                _detectionTimer = Mathf.Min(_detectionTimer + Time.deltaTime * _detectionSpeed, _detectionLevel);

                if (Target == null)
                {
                    // La cible n'est acquise qu'une fois le seuil atteint.
                    if (_detectionTimer >= _detectionLevel)
                    {
                        Target = _visibleTarget;
                        OnTargetSeen?.Invoke(Target);
                        OnDetectionCompleted?.Invoke(Target);
                    }
                }
                else
                {
                    // Garder la cible à jour tant qu'elle reste visible.
                    Target = _visibleTarget;
                }
            }
            else
            {
                // Oubli : le timer descend jusqu'à 0.
                _detectionTimer = Mathf.Max(_detectionTimer - Time.deltaTime * _forgettingSpeed, 0f);

                // La cible n'est perdue qu'une fois le timer vidé.
                if (_detectionTimer <= 0f && Target != null)
                {
                    Target = null;
                    OnTargetLost?.Invoke();
                }
            }

            // On ne notifie que si la valeur a réellement changé.
            if (!Mathf.Approximately(previousTimer, _detectionTimer))
                OnDetectionProgressChanged?.Invoke(DetectionProgress);
        }

        private Transform FindVisibleTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _targetMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform target = _buffer[i].transform;
                Vector3 targetPoint = _buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - EyePosition;

                Vector3 flatDir = new Vector3(dirToTarget.x, 0f, dirToTarget.z);

                if (flatDir.sqrMagnitude < 0.0001f) return target;

                if (Vector3.Angle(transform.forward, flatDir) > _angle * 0.5f) continue;

                if (Physics.Linecast(EyePosition, targetPoint, _obstructionMask, QueryTriggerInteraction.Ignore)) continue;

                return target;
            }

            return null;
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

            if (CanSeeTarget && _visibleTarget != null)
            {
                Gizmos.color = Target != null ? Color.green : Color.red;
                Gizmos.DrawLine(EyePosition, _visibleTarget.position);
            }
        }
#endif
    }
}