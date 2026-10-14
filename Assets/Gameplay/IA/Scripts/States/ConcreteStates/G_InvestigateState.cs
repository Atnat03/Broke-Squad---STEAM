using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.IA
{
    [Serializable]
    public class G_InvestigateState : G_BaseState
    {
        [Header("Moving to investigation position")]
        [SerializeField] private float _investigateSpeed = 1f;
        [SerializeField] private float _destinationRange = 1f;

        [Header("Investigate")]
        [Tooltip("Durée totale du balayage gauche-droite, en secondes.")]
        [SerializeField] private float _investigationDuration = 1f;
        [Tooltip("Angle en degrés de chaque côté du regard initial.")]
        [SerializeField] private float _investigateAngle = 15f;

        public event Action OnInvestigationEnded;
        public event Action OnFindPlayer;

        private Vector3 _investigatePosition;
        private bool _hasDestination;
        private bool _isInvestigating;
        private Coroutine _routine;

        public override void OnEnter()
        {
            _behavior.SetColor(_color);
            
            _isInvestigating = false;
            _routine = null;

            Transform target = _behavior.SuspectTarget;
            _hasDestination = target != null;

            if (!_hasDestination)
            {
                OnInvestigationEnded?.Invoke();
                return;
            }

            _investigatePosition = target.position;

            _motor.SetTarget(_investigatePosition);
            _motor.SetSpeed(_investigateSpeed);

            _lineOfSight.OnGetNewTarget += SeePlayerWhileInvestigate;
        }

        public override void OnUpdate()
        {
            if (!_hasDestination || _isInvestigating) return;

            Vector3 toTarget = _investigatePosition - _myTransform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= _destinationRange * _destinationRange)
            {
                _isInvestigating = true;
                _routine = _behavior.StartCoroutine(PerformInvestigation());
            }
        }

        public override void OnExit()
        {
            _lineOfSight.OnGetNewTarget -= SeePlayerWhileInvestigate;

            if (_routine != null) _behavior.StopCoroutine(_routine);

            _routine = null;
            _isInvestigating = false;
        }

        private void SeePlayerWhileInvestigate()
        {
            OnFindPlayer?.Invoke();
        }

        private IEnumerator PerformInvestigation()
        {
            _motor.SetTarget(_myTransform.position);

            yield return new WaitForSeconds(_investigationDuration * 0.33f);
            
            Quaternion startRotation = _myTransform.rotation;
            
            float duration = Mathf.Max(_investigationDuration * 0.66f, 0.01f);

            float firstPhase = 1f / 3f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float yaw = t < firstPhase
                    ? Mathf.Lerp(0f, -_investigateAngle, t / firstPhase)
                    : Mathf.Lerp(-_investigateAngle, _investigateAngle, (t - firstPhase) / (1f - firstPhase));

                _myTransform.rotation = startRotation * Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }

            _myTransform.rotation = startRotation * Quaternion.Euler(0f, _investigateAngle, 0f);

            _routine = null;
            OnInvestigationEnded?.Invoke();
        }
    }
}