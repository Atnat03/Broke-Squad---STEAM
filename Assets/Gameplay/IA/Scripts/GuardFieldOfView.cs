using System;
using UnityEngine;

namespace Gameplay.IA.Scripts
{
    public class GuardFieldOfView : MonoBehaviour
    {
        public bool CanSeeTarget { get; private set; }
        public Transform Target { get; private set; }

        public event Action<Transform> OnTargetSeen;
        public event Action OnTargetLost;

        [Header("Vision")]
        [SerializeField] private float _radius = 10f;
        [SerializeField, Range(0f, 360f)] private float _angle = 90f;
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _checkInterval = 0.2f;

        [Header("Layers")]
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LayerMask _obstructionMask;

        private readonly Collider[] _buffer = new Collider[8];
        private float _nextCheck;

        public float Radius => _radius;
        public float Angle => _angle;
        public Vector3 EyePosition => transform.position + Vector3.up * _eyeHeight;

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + _checkInterval;

            Transform found = FindVisibleTarget();
            bool seen = found != null;

            if (seen && !CanSeeTarget)
                OnTargetSeen?.Invoke(found);
            else if (!seen && CanSeeTarget)
                OnTargetLost?.Invoke();

            CanSeeTarget = seen;
            Target = found;
        }

        private Transform FindVisibleTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _radius, _buffer, _targetMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform target = _buffer[i].transform;
                Vector3 targetPoint = _buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - EyePosition;

                Vector3 flatDir = new Vector3(dirToTarget.x, 0f, dirToTarget.z);
                if (flatDir.sqrMagnitude < 0.0001f) return target;
                if (Vector3.Angle(transform.forward, flatDir) > _angle * 0.5f)
                    continue;

                if (Physics.Linecast(EyePosition, targetPoint, _obstructionMask, QueryTriggerInteraction.Ignore))
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

            Vector3 left = Quaternion.Euler(0, -_angle * 0.5f, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0, _angle * 0.5f, 0) * transform.forward;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + left * _radius);
            Gizmos.DrawLine(pos, pos + right * _radius);

            if (CanSeeTarget && Target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(EyePosition, Target.position);
            }
        }
#endif
    }
}