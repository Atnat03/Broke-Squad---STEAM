using System;
using UnityEngine;

namespace Gameplay.Controller
{
    public class KeyholeView : ICameraOverride
    {
        private readonly KeyholeTarget _t;
        private readonly Action _onSnap;
        private bool _released;
        private bool _snapped;
        private bool _lookInitialized;
        private float _timer;
        private float _yaw, _pitch;

        public KeyholeView(KeyholeTarget target, Action onSnap)
        {
            _t = target;
            _onSnap = onSnap;
        }

        public bool LocksController => !_released;
        public bool IsFinished => _released;
        public float Weight => _released ? 0f : _snapped ? 1f : Mathf.Min(0.99f, _timer / FadeTime);

        private float FadeTime => Mathf.Max(0.01f, _t.blendTime);

        public void OnEnter(ref float yaw, ref float pitch) { }

        public void ProcessLook(ref float yaw, ref float pitch)
        {
            if (!_snapped) return;  

            if (!_lookInitialized)
            {
                yaw = _t.BaseYaw;
                pitch = _t.BasePitch;
                _lookInitialized = true;
            }

            float yawOff = Mathf.Clamp(Mathf.DeltaAngle(_t.BaseYaw, yaw), -_t.maxYaw, _t.maxYaw);
            yaw = _t.BaseYaw + yawOff;
            pitch = Mathf.Clamp(pitch, _t.BasePitch - _t.maxPitch, _t.BasePitch + _t.maxPitch);
            _yaw = yaw;
            _pitch = pitch;
        }

        public void Tick(float dt)
        {
            if (_released || _snapped) return;

            _timer += dt;
            if (_timer >= FadeTime)
            {
                _snapped = true;
                _yaw = _t.BaseYaw;
                _pitch = _t.BasePitch;
                _onSnap?.Invoke();
            }
        }

        public CameraPose Apply(CameraPose normal)
        {
            if (!_snapped)
            {
                float p = Mathf.Clamp01(_timer / FadeTime);
                float t = _t.approachAmount * p * p;

                return new CameraPose
                {
                    position = Vector3.Lerp(normal.position, _t.viewPoint.position, t),
                    rotation = Quaternion.Slerp(normal.rotation, Quaternion.Euler(_t.BasePitch, _t.BaseYaw, 0f), t),
                    fov      = Mathf.Lerp(normal.fov, _t.fov, t)
                };
            }

            return new CameraPose
            {
                position = _t.viewPoint.position,
                rotation = Quaternion.Euler(_pitch, _yaw, 0f),
                fov      = _t.fov
            };
        }

        public void Release() => _released = true;
    }
}