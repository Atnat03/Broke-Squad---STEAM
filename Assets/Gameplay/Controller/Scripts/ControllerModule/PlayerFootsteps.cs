using System;
using Gameplay.Other.SuspiciousSound;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Gameplay.Controller
{
    [Serializable]
    public class PlayerFootsteps
    {
        
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float sprintStepInterval = 0.35f;
        [SerializeField] private float crouchStepInterval = 0.8f;
        [SerializeField] private float walkStepVolume = 0.3f;
        [SerializeField] private float sprintStepVolume = 0.5f;
        [SerializeField] private float crouchStepVolume = 0.1f;
        [SerializeField, SoundName] private string[] walkSound;

        [SerializeField] private SO_SuspiciousSoundSettings _settings;
        
        private PlayerController _controller;

        private Action<string, float> _play;
        private float _timer;
        private bool _wasMoving;

        public void Init(Action<string, float> play,  PlayerController controller)
        {
            _play = play;
            
            _controller = controller;
        }

        public void Tick(float dt, bool grounded, bool hasMoveInput, bool crouching, bool sprinting)
        {
            if (!grounded || !hasMoveInput || walkSound == null || walkSound.Length == 0)
            {
                _timer = 0f;
                _wasMoving = false;
                return;
            }

            float interval = crouching ? crouchStepInterval : sprinting ? sprintStepInterval : walkStepInterval;
            float volume = crouching ? crouchStepVolume : sprinting ? sprintStepVolume : walkStepVolume;

            if (!_wasMoving)
            {
                _wasMoving = true;
                _timer = 0f;
                PlayStep(volume);
                return;
            }

            _timer += dt;
            if (_timer >= interval)
            {
                _timer -= interval;
                PlayStep(volume);
            }
        }

        private void PlayStep(float volume)
        {
            _play?.Invoke(walkSound[Random.Range(0, walkSound.Length)], volume);

            if (_controller == null) return;
            _controller.SendEventSoundServerRpc(_settings.Force, _settings.MaxDistance);
            
        }
        
    }
}