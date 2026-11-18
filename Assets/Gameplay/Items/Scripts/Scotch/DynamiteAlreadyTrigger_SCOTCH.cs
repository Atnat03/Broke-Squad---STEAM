using System;
using System.Collections;
using Bus;
using Gameplay.LD;
using Gameplay.LD.Scripts;
using Gameplay.Other.SuspiciousSound;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.Scotch
{
    public class DynamiteAlreadyTrigger_SCOTCH : NetworkBusListener
    {
        [SerializeField] private string _eventKey = "TRIGGER_ALREADY_SCOTCH";
        
        [Header("Explosion Settings")] 
        [SerializeField] private float _timerToExplose = 5f;

        [SerializeField] private float _explosionRadius = 3f;
        [SerializeField] private float _explosionDamage = 45f;
        [SerializeField] private float _explosionBreakForce = 100f;

        [Header("Camera shake")] [SerializeField]
        private float _shakeDuration = 0.25f;

        [SerializeField] private float _shakeMagnitude = 0.5f;

        [Header("Suspicious Sound Settings")] [SerializeField]
        private SO_SuspiciousSoundSettings _suspiciousSoundSettings;

        [Header("SFX")] [SerializeField, SoundName]
        private string _sfxTrigger;

        [SerializeField, SoundName] private string _sfxExplosion;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;

        private float _elapsed;
        
        private void Start()
        {
            ABPrint.Print("Start");
            StartExplosion();
        }

        void StartExplosion()
        {
            ABPrint.Print("Start explosion");
            
            _elapsed = 0;
            StartCoroutine(ExplosionRoutine());
        }

        IEnumerator ExplosionRoutine()
        {
            ABPrint.Print("ExplosionRoutine");
            
            SendData(true);

            while (_elapsed < _timerToExplose)
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
            var size = Physics.OverlapSphereNonAlloc(transform.position, _explosionRadius, buffer);

            for (int i = 0; i < size; i++)
            {
                IDamageable d = buffer[i].GetComponentInChildren<IDamageable>();
                d?.ApplyDamage(_explosionDamage);

                if (buffer[i].TryGetComponent(out IBreakable breakable))
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
        }

        private void SendData(bool state)
        {
            InvokeEvent(new OnModuleDoAction_EVENT
            {
                KeyEvent = _eventKey,
                ValueB = state,
                ValueF = _elapsed / _timerToExplose,
                HostId = ulong.MinValue,
            });

        }
    }
}