using System;
using System.Collections;
using System.Collections.Generic;
using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Gameplay.LD
{
    [RequireComponent(typeof(NetworkTransform))]
    public class MovingPlatform : NetworkBusListener
    {
        [Header("Settings")]
        [SerializeField] private Transform _downPosition;
        [SerializeField] private Transform _upPosition;
        [SerializeField] private AnimationCurve _movingCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private float _duration = 4;
        private Coroutine _movingCoroutine;
        
        [SerializeField] private bool _up;

        public void ActivateMoving()
        {
            if (!IsServer)
            {
                ActivateMovingServerRpc();
                return;
            }
            
            _up = !_up;

            if (_movingCoroutine != null)
                StopCoroutine(_movingCoroutine);

            _movingCoroutine = StartCoroutine(MovingRoutine());
        }

        [Rpc(SendTo.Server)]
        private void ActivateMovingServerRpc() => ActivateMoving();
        

        private IEnumerator MovingRoutine()
        {
            Transform startT = _up ? _downPosition : _upPosition;
            Transform endT = _up ? _upPosition : _downPosition;
            
            float e = 0;

            while (e < _duration)
            {
                e += Time.deltaTime;

                float t = Mathf.Clamp01(e / _duration);
                float curveValue = _movingCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startT.position, endT.position, curveValue);
                
                yield return null;
            }

            transform.position = endT.position;

            _movingCoroutine = null;
        }


        void OnTriggerEnter(Collider c) {
            if (!IsServer) return;
            var p = c.GetComponent<PlayerController>();
            if (p != null && p.NetworkObject.IsPlayerObject)
            {
                p.NetworkObject.TrySetParent(NetworkObject);
            }
        }
        void OnTriggerExit(Collider c) {
            if (!IsServer) return;
            var p = c.GetComponent<PlayerController>();
            if (p != null && p.NetworkObject.IsPlayerObject)
            {
                p.NetworkObject.TryRemoveParent();
            }
        }
    }
}