using System.Collections;
using Gameplay.Other.SuspiciousSound;
using UnityEngine;

namespace Gameplay.Items
{
    public class ActiveExplosionModule : ActionModule, IFirstAction, IServerAction, IAnchorAware
    {
        [SerializeField] private string _keyEvent = "EXPLOSION_EVENT";
        [SerializeField] private float _timerToExplose = 5f;
        
        [Header("Suspicious Sound Settings")]
        [SerializeField] private SO_SuspiciousSoundSettings _suspiciousSoundSettings;

        [Header("SFX")] 
        [SerializeField, SoundName] private string _sfxTrigger;
        [SerializeField, SoundName] private string _sfxExplosion;
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
                soundName = _sfxTrigger
            });
        }

        public void StopFirstAction() { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            if (!_active)
                StartExplosion();
        }

        private void StartExplosion()
        {
            _active = true;
            _elapsed = 0;
            Context.StartCoroutine(ExplosionRoutine(++_runId));
        }

        IEnumerator ExplosionRoutine(int id)
        {
            SendData(true);

            float tick = 0f;
            while (_active && id == _runId && _elapsed < _timerToExplose)
            {
                SendData(true);
                
                yield return null;
            }

            PerformExplosion();
        }

        private void PerformExplosion()
        {
            SendData(false);
        }

        public void SetAnchor(Transform anchor)
        {
            _anchor = anchor;
            if (_active) SendData(true);
        }
        
        public void SetAnchor(Transform anchor, ulong hostId)
        {
            _anchor = anchor;
            _hostId = hostId;
            if (_active) SendData(true);
        }
        
        private void SendData(bool state) => Context.SendEventToEveryone(new OnModuleDoAction_EVENT
        {
            KeyEvent = _keyEvent,
            ValueB = state,
            ValueF = _elapsed,
            HostId = _hostId
        });
    }
}