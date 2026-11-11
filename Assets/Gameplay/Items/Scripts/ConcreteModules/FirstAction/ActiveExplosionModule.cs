using System;
using System.Collections;
using Gameplay.Controller;
using Gameplay.LD;
using Gameplay.LD.Scripts;
using Gameplay.Other.SuspiciousSound;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ActiveExplosionModule : ActionModule, IFirstAction, IServerAction, IAnchorAware
    {
        [SerializeField] private string _keyEvent = "EXPLOSION_EVENT";
     
        [Header("Explosion Settings")]
        [SerializeField] private float _timerToExplose = 5f;
        [SerializeField] private float _explosionRadius = 3f;
        [SerializeField] private float _explosionDamage = 45f;
        [SerializeField] private float _explosionBreakForce = 100f;
        
        [Header("Camera shake")]
        [SerializeField] private float _shakeDuration = 0.25f;
        [SerializeField] private float _shakeMagnitude = 0.5f;
        
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

        private Coroutine _explosionCoroutine;
        
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

        public void OnNotInHand()
        {
            if (_explosionCoroutine == null) return;
            
            Context.StopCoroutine(_explosionCoroutine);
            _explosionCoroutine = null;

            _active = false;
            _elapsed = 0;
            
            SendData(true);
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            if (!_active)
                StartExplosion();
        }

        private void StartExplosion()
        {
            _active = true;
            _elapsed = 0;
            _explosionCoroutine = Context.StartCoroutine(ExplosionRoutine(++_runId));
        }

        IEnumerator ExplosionRoutine(int id)
        {
            SendData(true);
            
            while (_active && id == _runId && _elapsed < _timerToExplose)
            {
                _elapsed += Time.deltaTime;
                
                SendData(true);
                
                yield return null;
            }

            PerformExplosion();
        }

        private void PerformExplosion()
        {
            Collider[] buffer = new Collider[32];
            var size = Physics.OverlapSphereNonAlloc(Context.Core.Instance.Anchor.position, _explosionRadius, buffer);

            for (int i = 0; i < size; i++)
            {
                if (buffer[i].TryGetComponent(out IDamageable damageable))
                {
                    damageable.ApplyDamage(_explosionDamage);
                }
                
                if(buffer[i].TryGetComponent(out IBreakable breakable))
                {
                    breakable.Break(_explosionBreakForce);
                }

                /*if (buffer[i].TryGetComponent(out PlayerController player))
                {
                    Context.Inventory.InvokeEvent(new OnCameraShakeEvent
                    {
                        PlayerClientId = player.OwnerClientId,
                        Duration = _shakeDuration,
                        Magnitude = _shakeMagnitude,
                    });
                }*/
            }

            SendData(false);
            
            if (Context.Core.Instance == Context.Inventory.GetCurrentItemInHand())
            {
                Context.Inventory.ClearCurrentSlot();
            }
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
            ValueF = _elapsed / _timerToExplose,
            HostId = _hostId
        });
    }
}