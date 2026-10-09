
using System;
using UnityEngine;

namespace Gameplay.IA.Scripts
{
    public class GuardFieldOfView : MonoBehaviour
    {
        public bool CanSeeTarget { get; private set; }
        public Transform Target { get; private set; }

        public float DetectionProgress { get; private set; }

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
        [SerializeField] private float _detectionDuration = 2f;

        [Header("Layers")]
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LayerMask _obstructionMask;

        private readonly Collider[] _buffer = new Collider[8];

        private float _nextCheck;
        private float _detectionTimer;
        private Transform _visibleTarget;

        public float Radius => _radius;
        public float Angle => _angle;
        public Vector3 EyePosition =>
            transform.position + Vector3.up * _eyeHeight;

        private void Update()
        {
            if (Time.time < _nextCheck)
                return;

            float elapsed = _nextCheck == 0f
                ? _checkInterval
                : Time.time - (_nextCheck - _checkInterval);

            _nextCheck = Time.time + _checkInterval;

            Transform found = FindVisibleTarget();
            CanSeeTarget = found != null;

            if (found == null)
            {
                _visibleTarget = null;
                _detectionTimer = 0f;
                DetectionProgress = 0f;

                if (Target != null)
                {
                    Target = null;
                    OnTargetLost?.Invoke();
                }

                OnDetectionProgressChanged?.Invoke(DetectionProgress);
                return;
            }

            // Une nouvelle cible démarre une nouvelle détection.
            if (_visibleTarget != found)
            {
                _visibleTarget = found;
                _detectionTimer = 0f;
                DetectionProgress = 0f;
            }

            // La cible n'est acquise qu'après le délai.
            if (Target == null)
            {
                _detectionTimer += elapsed;

                DetectionProgress = _detectionDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(_detectionTimer / _detectionDuration);

                OnDetectionProgressChanged?.Invoke(DetectionProgress);

                if (DetectionProgress >= 1f)
                {
                    Target = found;
                    OnTargetSeen?.Invoke(Target);
                    OnDetectionCompleted?.Invoke(Target);
                }
            }
            else
            {
                // Garder la cible à jour tant qu'elle reste visible.
                Target = found;
            }
        }

        private Transform FindVisibleTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _buffer,
                _targetMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform target = _buffer[i].transform;
                Vector3 targetPoint = _buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - EyePosition;

                Vector3 flatDir = new Vector3(
                    dirToTarget.x, 0f, dirToTarget.z);

                if (flatDir.sqrMagnitude < 0.0001f)
                    return target;

                if (Vector3.Angle(transform.forward, flatDir) > _angle * 0.5f)
                    continue;

                if (Physics.Linecast(
                    EyePosition,
                    targetPoint,
                    _obstructionMask,
                    QueryTriggerInteraction.Ignore))
                    continue;

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

            Vector3 left = Quaternion.Euler(0, -_angle * 0.5f, 0)
                           * transform.forward;
            Vector3 right = Quaternion.Euler(0, _angle * 0.5f, 0)
                            * transform.forward;

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