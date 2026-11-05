using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardLineSight : NetworkBehaviour
    {
        // Player
        public bool CanSeeTarget => _visibleTargetPlayer != null;
        public Vector3 LastSeenPosition { get; private set; }

        // Suspect
        public bool CanSeeSuspect => _visibleTargetSuspect != null;
        public Vector3 LastSeenSuspectPosition { get; private set; }

        //Actions
        public event Action<float> OnDetectionProgressChanged;
        public event Action<float> OnSuspectDetectionProgressChanged;

        [Header("Settings")]
        private Transform _finalTarget;
        private Transform _finalSuspectTarget;

        private Transform _visibleTargetPlayer;
        private Transform _visibleTargetSuspect;

        private float _nextCheckPlayer;
        private float _nextCheckSuspect;

        public Vector3 EyePosition => transform.position + Vector3.up * Brain.CurrentSetting.EyeHeight;

        //Buffer for overlap all
        private readonly Collider[] _bufferPlayer = new Collider[8];
        private readonly Collider[] _bufferSuspect = new Collider[8];

        [Header("Detection Timer NV")]
        private readonly NetworkVariable<float> _detectionTimer = new();
        private readonly NetworkVariable<float> _detectionSuspectTimer = new();

        public override void OnNetworkSpawn()
        {
            _detectionTimer.OnValueChanged += OnDetectionTimerChange;
            _detectionSuspectTimer.OnValueChanged += OnDetectionSuspectTimerChange;
        }

        public override void OnNetworkDespawn()
        {
            _detectionTimer.OnValueChanged -= OnDetectionTimerChange;
            _detectionSuspectTimer.OnValueChanged -= OnDetectionSuspectTimerChange;
        }

        //Brain reference
        public GuardBrain Brain { get; set; }
        public void Initialise(GuardBrain brain) => Brain = brain;
        

        // Check if Player is in front of him
        public Transform PerformFindingPlayer(Transform forcedTarget = null)
        {
            if (Time.time >= _nextCheckPlayer)
            {
                _nextCheckPlayer = Time.time + Brain.CurrentSetting.CheckInterval;
                _visibleTargetPlayer = FindVisible(Brain.CurrentSetting.TargetPlayerMask, _bufferPlayer);
            }

            return UpdatePlayerDetection(forcedTarget);
        }

        // Check if suspect object is in front of him
        public Transform PerformFindingSuspect(Transform forcedTarget = null)
        {
            if (Time.time >= _nextCheckSuspect)
            {
                _nextCheckSuspect = Time.time + Brain.CurrentSetting.CheckInterval;
                _visibleTargetSuspect = FindVisible(Brain.CurrentSetting.TargetSuspectMask, _bufferSuspect);
            }

            return UpdateSuspectDetection(forcedTarget);
        }

        private Transform UpdatePlayerDetection(Transform forcedTarget)
        {
            if (_visibleTargetPlayer)
            {
                LastSeenPosition = _visibleTargetPlayer.position;

                float level = Brain.CurrentSetting.DetectionLevel;
                float speed = Brain.CurrentSetting.DetectionSpeed;

                _detectionTimer.Value = Mathf.Min(_detectionTimer.Value + Time.deltaTime * speed, level);

                if (!_finalTarget)
                {
                    if (_detectionTimer.Value >= level)
                    {
                        _finalTarget = _visibleTargetPlayer;
                    }
                }
                else
                {
                    _finalTarget = _visibleTargetPlayer;
                }
            }
            else if (forcedTarget)
            {
                LastSeenPosition = forcedTarget.position;
                _finalTarget = forcedTarget;
                _detectionTimer.Value = Brain.CurrentSetting.DetectionLevel;
            }
            else
            {
                _detectionTimer.Value = Mathf.Max(_detectionTimer.Value - Time.deltaTime * Brain.CurrentSetting.ForgettingSpeed, 0f);

                if (_detectionTimer.Value <= 0f && _finalTarget)
                {
                    _finalTarget = null;
                }
            }

            return _finalTarget;
        }

        private Transform UpdateSuspectDetection(Transform forcedTarget)
        {
            if (_visibleTargetSuspect)
            {
                LastSeenSuspectPosition = _visibleTargetSuspect.position;

                float level = Brain.CurrentSetting.DetectionSuspectLevel;
                float speed = Brain.CurrentSetting.DetectionSuspectSpeed;

                _detectionSuspectTimer.Value = Mathf.Min(_detectionSuspectTimer.Value + Time.deltaTime * speed, level);

                if (!_finalSuspectTarget)
                {
                    if (_detectionSuspectTimer.Value >= level)
                    {
                        _finalSuspectTarget = _visibleTargetSuspect;
                    }
                }
                else
                {
                    _finalSuspectTarget = _visibleTargetSuspect;
                }
            }
            else if (forcedTarget)
            {
                LastSeenSuspectPosition = forcedTarget.position;
                _finalSuspectTarget = forcedTarget;
                _detectionSuspectTimer.Value = Brain.CurrentSetting.DetectionSuspectLevel;
            }
            else
            {
                _detectionSuspectTimer.Value = Mathf.Max(_detectionSuspectTimer.Value - Time.deltaTime * Brain.CurrentSetting.ForgettingSuspectSpeed, 0f);

                if (_detectionSuspectTimer.Value <= 0f && _finalSuspectTarget)
                {
                    _finalSuspectTarget = null;
                }
            }

            return _finalSuspectTarget;
        }

        private Transform FindVisible(LayerMask mask, Collider[] buffer)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, Brain.CurrentSetting.Radius, buffer, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform target = buffer[i].transform;
                Vector3 targetPoint = buffer[i].bounds.center;
                Vector3 dirToTarget = targetPoint - EyePosition;

                Vector3 flatDir = new Vector3(dirToTarget.x, 0f, dirToTarget.z);

                if (flatDir.sqrMagnitude < 0.0001f) return target;

                if (Vector3.Angle(transform.forward, flatDir) > Brain.CurrentSetting.Angle * 0.5f) continue;

                if (Physics.Linecast(EyePosition, targetPoint, Brain.CurrentSetting.ObstructionMask, QueryTriggerInteraction.Ignore)) continue;

                return target;
            }

            return null;
        }

        #region NetworkVariabes Changes

        private void OnDetectionTimerChange(float previousValue, float newValue)
        {
            if (Brain == null) return;

            if (Mathf.Abs(previousValue - newValue) > 0.001f)
            {
                float level = Brain.CurrentSetting.DetectionLevel;

                OnDetectionProgressChanged?.Invoke(level > 0f ? Mathf.Clamp01(newValue / level) : 1f);
            }
        }

        private void OnDetectionSuspectTimerChange(float previousValue, float newValue)
        {
            if (Brain == null) return;

            if (Mathf.Abs(previousValue - newValue) > 0.001f)
            {
                float level = Brain.CurrentSetting.DetectionSuspectLevel;

                OnSuspectDetectionProgressChanged?.Invoke(level > 0f ? Mathf.Clamp01(newValue / level) : 1f);
            }
        }

        #endregion
    }
}