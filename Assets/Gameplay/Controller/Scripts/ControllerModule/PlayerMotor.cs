using System;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerMotor
    {
        private readonly Rigidbody _rb;
        private readonly Transform _transform;
        private readonly ControllerProfileSO _profile;
        private readonly GroundChecker _ground;
        private readonly PlayerInputReader _input;

        private float _jumpBufferTimer;
        private float _coyoteTimer;

        public float TargetSpeed { get; set; }
        public bool JumpAllowed { get; set; }

        public Vector3 HorizontalVelocity => new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        public float ForwardDot
        {
            get
            {
                Vector3 v = HorizontalVelocity;
                return v.sqrMagnitude < 0.01f ? 0f : Vector3.Dot(v.normalized, _transform.forward);
            }
        }

        public Action OnJump;

        public PlayerMotor(Rigidbody rb, Transform transform, ControllerProfileSO profile,
            GroundChecker ground, PlayerInputReader input)
        {
            _rb = rb;
            _transform = transform;
            _profile = profile;
            _ground = ground;
            _input = input;
        }

        public void BufferJump() => _jumpBufferTimer = _profile.jumpBufferTime;

        public void StopMoving()
        {
            _rb.linearVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
        }

        public void FixedTick(float dt)
        {
            _coyoteTimer = _ground.IsGrounded ? _profile.coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;

            ApplyHorizontal(dt);
            ApplyJump();
            ApplyGravity(dt);
        }

        private void ApplyHorizontal(float dt)
        {
            float speed = TargetSpeed * (ForwardDot < -0.1f ? _profile.backwardSpeedMultiplier : 1f);

            Vector3 input = Vector3.ClampMagnitude(new Vector3(_input.Move.x, 0f, _input.Move.y), 1f);
            Vector3 target = _transform.TransformDirection(input) * speed;

            float accel = _ground.IsGrounded
                ? (input.sqrMagnitude > 0.01f ? _profile.groundAcceleration : _profile.groundDeceleration)
                : _profile.airAcceleration;

            Vector3 v = _rb.linearVelocity;
            Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), target, accel * dt);
            _rb.linearVelocity = new Vector3(h.x, v.y, h.z);
        }

        private void ApplyJump()
        {
            if (!JumpAllowed || _jumpBufferTimer <= 0f || _coyoteTimer <= 0f) return;

            float g = Mathf.Abs(Physics.gravity.y) * _profile.gravityMultiplier;
            float jumpVel = Mathf.Sqrt(2f * g * _profile.jumpHeight);

            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVel, _rb.linearVelocity.z);

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _ground.ForceAirborne();
            OnJump?.Invoke();
        }

        private void ApplyGravity(float dt)
        {
            Vector3 v = _rb.linearVelocity;
            bool noInput = _input.Move.sqrMagnitude < 0.01f;

            if (_ground.IsGrounded && noInput && v.y <= 0.01f) return;

            float mult = _profile.gravityMultiplier * (v.y < 0f ? _profile.fallGravityMultiplier : 1f);
            v.y += Physics.gravity.y * mult * dt;
            _rb.linearVelocity = v;
        }
    }
}