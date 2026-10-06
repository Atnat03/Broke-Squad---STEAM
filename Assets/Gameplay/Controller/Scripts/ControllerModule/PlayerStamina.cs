using System;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerStamina
    {
        private readonly ControllerProfileSO _profile;
        private readonly Action<float, float> _onChangedStamina;

        private float _regenTimer;
        private float _lastSent = -1f;
        private bool _drainedThisTick;

        public float Current { get; private set; }
        public float Max => _profile.maxStamina;
        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && Current > 0f;

        public PlayerStamina(ControllerProfileSO profile, Action<float, float> onChanged)
        {
            _profile = profile;
            _onChangedStamina = onChanged;
            Current = Max;
        }
        
        public void Drain(float dt)
        {
            Current = Mathf.Max(0f, Current - _profile.drainPerSecond * dt);
            _regenTimer = _profile.regenDelay;
            _drainedThisTick = true;
            if(Current <= 0f) Exhausted = true;
        }

        public void Tick(float dt)
        {
            if (!_drainedThisTick)
            {
                if(_regenTimer > 0f) _regenTimer -= dt;
                else Current = Mathf.Min(Max, Current + _profile.regenPerSecond * dt);
            }
            _drainedThisTick = false; 
            
            if(Exhausted && Current > 0f) Exhausted = false;
            Notify();
        }

        private void Notify()
        {
            bool changedEnough = Mathf.Abs(Current - _lastSent) >= Max * 0.01f;
            bool atBoundary = (Current <= 0f || Current >= Max) && !Mathf.Approximately(Current, _lastSent);
            if (!changedEnough && !atBoundary) return;

            _lastSent = Current;
            _onChangedStamina?.Invoke(Current, Max);
        }
    }
}