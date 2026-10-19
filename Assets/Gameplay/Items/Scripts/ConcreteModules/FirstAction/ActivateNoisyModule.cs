using System;
using System.Collections;
using Gameplay.Items;
using Gameplay.Other.SuspiciousSound;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ActivateNoisyModule : ActionModule, IFirstAction, IServerAction, IAnchorAware
    {
        [SerializeField] private string _keyEvent = "NOISY_EVENT";
        [SerializeField] private float _maxDuration = 5f;

        [Header("Suspicious Sound Settings")]
        [SerializeField] private float _suspectSoundSendIntervale = 1f;
        [SerializeField] private SO_SuspiciousSoundSettings _suspiciousSoundSettings;

        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        private float _elapsed;
        private bool _active;
        private int _runId;
        private Transform _anchor;
        private Vector3 _lastPosition;

        private ulong _hostId;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT());
            
            //SFX
            Context.Inventory.InvokeEvent(new PlaySoundEvent
            {
                position = Context.Core.transform.position,
                volume = _volume,
                soundName = _stringName
            });
        }
        
        public void StopFirstAction() { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            if (_active) StopNoisy();
            else StartNoisy();
        }

        public void SetAnchor(Transform anchor)
        {
            _anchor = anchor;
            if (_active) SendData(true);
        }

        private void StartNoisy()
        {
            _active = true;
            _elapsed = 0f;
            Context.StartCoroutine(NoisyCoroutine(++_runId));
        }

        private void StopNoisy()
        {
            _active = false;
            _runId++;
            SendData(false);
        }

        private IEnumerator NoisyCoroutine(int id)
        {
            SendData(true);

            float tick = 0f;
            while (_active && id == _runId && _elapsed < _maxDuration)
            {
                _elapsed += Time.deltaTime;
                tick += Time.deltaTime;

                if (tick > _suspectSoundSendIntervale)
                {
                    Context.SendSuspiciousSound(Context.Inventory.OwnerClientId, CurrentPosition(), _suspiciousSoundSettings);
                    SendData(true);
                    tick = 0f;
                }

                yield return null;
            }

            if (_active && id == _runId) StopNoisy();
        }

        private Vector3 CurrentPosition()
        {
            if (_anchor != null) _lastPosition = _anchor.position;
            else if (Context.Inventory != null) _lastPosition = Context.Inventory.transform.position;
            return _lastPosition;
        }

        private void SendData(bool state) => Context.SendEventToEveryone(new OnModuleDoAction_EVENT
        {
            KeyEvent = _keyEvent,
            ValueB = state,
            ValueF = _elapsed,
            HostId = _hostId
        });
        
        public void SetAnchor(Transform anchor, ulong hostId)
        {
            _anchor = anchor;
            _hostId = hostId;
            if (_active) SendData(true);
        }
    }
}