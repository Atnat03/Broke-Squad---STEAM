using System;
using Gameplay.IA.Refacto.States;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA.Refacto
{
    public class GuardLineSight : NetworkBehaviour
    {
        public bool CanSeeTarget => _visibleTargetPlayer != null;
        public Vector3 LastSeenPosition { get; private set; }
        
        //Actions
        public event Action<float> OnDetectionProgressChanged;
        
        [Header("Settings")]
        private Transform _finalTarget;
        private Transform _visibleTargetPlayer;
        private float _nextCheck;
        Vector3 EyePosition => transform.position + Vector3.up * Brain.CurrentSetting.EyeHeight;
        private readonly Collider[] _bufferPlayer = new Collider[8];
        
        [Header("Detection Timer NV")]
        private readonly NetworkVariable<float> _detectionTimer = new();

        public override void OnNetworkSpawn()
        {
            _detectionTimer.OnValueChanged += OnDetectionTimerChange;
        }

        public override void OnNetworkDespawn()
        {
            _detectionTimer.OnValueChanged -= OnDetectionTimerChange;
        }
        
        //Brain reference        
        public GuardBrain Brain { get; set; }
        public void Initialise(GuardBrain brain) => Brain = brain;
        
        // Check if Player is in front of him
        public Transform PerformFindingPlayer()
        {
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + Brain.CurrentSetting.CheckInterval;
                _visibleTargetPlayer = FindVisible(Brain.CurrentSetting.TargetPlayerMask, _bufferPlayer);
            }
            
            return UpdatePlayerDetection();
        }

        private Transform UpdatePlayerDetection()
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
            if (Mathf.Abs(previousValue - newValue) > 0.001f)
            {
                float level = Brain.CurrentSetting.DetectionLevel;
                
                OnDetectionProgressChanged?.Invoke(level > 0f ? Mathf.Clamp01(newValue / level) : 1f);
            }
        }
        
        #endregion
    }
    
}