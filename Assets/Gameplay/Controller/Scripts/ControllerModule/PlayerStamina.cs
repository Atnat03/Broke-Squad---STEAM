

using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerStamina
    {
        private readonly ControllerProfileSO _profile;
        private readonly Action<float, float> _onChangedStamina;

        private float  _overrideStaminaRegen = -1;
        private float _overrideStaminaDelay = -1;
        private float _regenTimer;
        private float _lastSent = -1f;
        private bool _drainedThisTick;
        
        private float EffectiveRegen => _overrideStaminaRegen >= 0f ? _overrideStaminaRegen : _profile.regenPerSecond;
        private float EffectiveDelay => _overrideStaminaDelay >= 0f ? _overrideStaminaDelay : _profile.regenDelay;

        public float Current { get; private set; }
        public float Max => _profile.maxStamina;
        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && Current > 0f;

        public PlayerStamina(ControllerProfileSO profile, Action<float, float> onChanged)
        {
            _profile = profile;
            _onChangedStamina = onChanged;
            Current = Max;
            _onChangedStamina.Invoke(Current, Max);
        }
        
        public void Drain(float dt)
        {
            Current = Mathf.Max(0f, Current - _profile.drainPerSecond * dt);
            _regenTimer = EffectiveDelay;
            _drainedThisTick = true;
            //if(Current <= 0f) Exhausted = true;
        }

        public void Tick(float dt)
        {
            if (!_drainedThisTick)
            {
                if(_regenTimer > 0f) _regenTimer -= dt;
                else Current = Mathf.Min(Max, Current + EffectiveRegen * dt);
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

        public void GainStamina(float value)
        {
            Current = Mathf.Min(Max, Current + value);
            Notify();
        }

        public void OverrideStaminaRegenAndDelay(float newRegen, float newDelay)
        {
            _overrideStaminaRegen = newRegen;
            _overrideStaminaDelay = newDelay;
            
            if (newDelay >= 0f) _regenTimer = Mathf.Min(_regenTimer, newDelay);
        }

       
    }
}