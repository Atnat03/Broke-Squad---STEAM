using System;
using UnityEngine;

namespace Gameplay.Controller
{
    [Serializable]
    public class PlayerBody
    {
        [SerializeField] private LayerMask ceilingLayer;
        [SerializeField] private Transform eyeTarget;
        [SerializeField] private Transform visual;
        [SerializeField] private GameObject tiedVisual;
        [SerializeField] private float tiedUpHeight = 0.8f;

        private Transform _root;
        private CapsuleCollider _capsule;
        private ControllerProfileSO _profile;

        private Vector3 _visualBaseScale = Vector3.one;
        private float _initialHeight;
        private float _bottomOffsetY;
        private float _eyeStandLocalY;
        private float _targetHeight;

        public float StandHeight => _profile.standHeight;
        public float CrouchHeight => _profile.crouchHeight;
        public float TiedUpHeight => tiedUpHeight;
        public float TargetHeight => _targetHeight;

        public float ScaleY => Mathf.Abs(_root.lossyScale.y);
        public float WorldRadius => _capsule.radius * Mathf.Max(Mathf.Abs(_root.lossyScale.x), Mathf.Abs(_root.lossyScale.z));
        
        public Vector3 FeetPosition
        {
            get
            {
                Vector3 c = _root.TransformPoint(_capsule.center);
                float effectiveHeight = Mathf.Max(_capsule.height, _capsule.radius * 2f);
                return new Vector3(c.x, c.y - effectiveHeight * 0.5f * ScaleY, c.z);
            }
        }

        public void Init(Transform root, CapsuleCollider capsule, ControllerProfileSO profile)
        {
            _root = root;
            _capsule = capsule;
            _profile = profile;
            
            _bottomOffsetY = capsule.center.y - capsule.height / 2f;
            _initialHeight = _capsule.height;
            if(visual != null) _visualBaseScale = visual.localScale;
            
            _targetHeight = profile.standHeight;
            capsule.height = _targetHeight;
            Recenter();
            
            if (eyeTarget != null) _eyeStandLocalY = eyeTarget.localPosition.y;
            else Debug.LogWarning("PlayerBody: eyeTarget is not assigned, the camera won't lower when crouching.");
            
            SetTied(false);
        }
        
        public void SetTargetHeight(float height) => _targetHeight = height;
        
        public void SetTied(bool tied)
        {
            if (tiedVisual != null) tiedVisual.SetActive(tied);
        }

        public void Tick(float dt)
        {
            _capsule.height = Mathf.MoveTowards(_capsule.height, _targetHeight, _profile.crouchTransitionSpeed * dt);
            Recenter();

            if (visual == null) return;
            float ratio = _capsule.height / _initialHeight;
            visual.localScale = new Vector3(_visualBaseScale.x, _visualBaseScale.y * ratio, _visualBaseScale.z);
            Vector3 pos = visual.localPosition;
            pos.y = _bottomOffsetY + _capsule.height /2f;
            visual.localPosition = pos;
        }

        //Only called on non-owner
        public void ApplyRemote(float dt, bool crouching, bool down)
        {
            _targetHeight = down ? tiedUpHeight : crouching ? _profile.crouchHeight : _profile.standHeight;
            SetTied(down);
            Tick(dt);         
        }

        public void UpdateEye(float dt)
        {
            if(eyeTarget == null) return;
            float targetY = _eyeStandLocalY - (_profile.standHeight - _targetHeight);
            Vector3 p = eyeTarget.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetY, _profile.crouchTransitionSpeed * dt);
            eyeTarget.localPosition = p;
        }

        public bool CanStandUp()
        {
            float radius = WorldRadius * 0.95f;
            Vector3 feet = FeetPosition;
            Vector3 bottom = feet + Vector3.up * (radius + 0.05f);
            Vector3 top = feet + Vector3.up * (_profile.standHeight * ScaleY - radius);
            Debug.DrawLine(bottom, top, Color.red);
            return !Physics.CheckCapsule(bottom, top, radius, ceilingLayer, QueryTriggerInteraction.Ignore);
        }
        
        private void Recenter()
        {
            Vector3 center = _capsule.center;
            center.y = _bottomOffsetY + _capsule.height / 2f;
            _capsule.center = center;
        }
    }
}