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
        private readonly StepClimber _steps;

        private float _jumpBufferTimer;
        private float _coyoteTimer;

        public float TargetSpeed { get; set; }
        public bool JumpAllowed { get; set; }
        
        public bool CanMove { get; set; } = true;
        public bool IsClimbing { get; set; }
        public float ClimbVelocity { get; set; }
        private Vector2 MoveInput => CanMove ? _input.Move : Vector2.zero;

        public Vector3 HorizontalVelocity => new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        public float ForwardDot
        {
            get
            {
                Vector3 v = HorizontalVelocity;
                return v.sqrMagnitude < 0.01f ? 0f : Vector3.Dot(v.normalized, _transform.forward);
            }
        }
        
        public Vector3 DesiredDirection
        {
            get
            {
                Vector3 i = Vector3.ClampMagnitude(new Vector3(MoveInput.x, 0f, MoveInput.y), 1f);
                return _transform.TransformDirection(i);
            }
        }

        public Action OnJump;
        
        

        

        public PlayerMotor(Rigidbody rb, Transform transform, ControllerProfileSO profile,
            GroundChecker ground, PlayerInputReader input, PlayerBody body)
        {
            _rb = rb;
            _transform = transform;
            _profile = profile;
            _ground = ground;
            _input = input;
            _steps = new StepClimber(rb, body, ground, profile);
        }
        
        public void BufferJump() => _jumpBufferTimer = _profile.jumpBufferTime;
        public void LaunchOffLadderTop(Vector3 forward)
        {
            float g = Mathf.Abs(Physics.gravity.y) * _profile.gravityMultiplier;
            float liftVel = Mathf.Sqrt(2f * g * _profile.ladderTopLift);

            forward.y = 0f;
            Vector3 push = forward.normalized * _profile.ladderTopPushSpeed;

            _rb.linearVelocity = new Vector3(push.x, liftVel, push.z);
            _ground.ForceAirborne();
        }
        public void StopMoving()
        {
            _rb.linearVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
        }

        public void FixedTick(float dt)
        {
            if (IsClimbing)
            {
                _rb.linearVelocity = new Vector3(0f, ClimbVelocity, 0f);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                return;
            }
            
            _coyoteTimer = _ground.IsGrounded ? _profile.coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;

            _steps.Tick(DesiredDirection); 
            ApplyHorizontal(dt);
            ApplyJump();
            ApplyGravity(dt);
            SnapToGround();
        }

        private void ApplyHorizontal(float dt)
        {
            float speed = TargetSpeed * (ForwardDot < -0.1f ? _profile.backwardSpeedMultiplier : 1f);

            Vector3 input = Vector3.ClampMagnitude(new Vector3(MoveInput.x, 0f, MoveInput.y), 1f);
            Vector3 worldDir = _transform.TransformDirection(input);
            bool grounded = _ground.IsGrounded;

            float accel = grounded
                ? (input.sqrMagnitude > 0.01f ? _profile.groundAcceleration : _profile.groundDeceleration)
                : _profile.airAcceleration;

            Vector3 v = _rb.linearVelocity;

            if (grounded)
            {
                Vector3 normal = _ground.Normal;
                
                Vector3 slopeDir = Vector3.ProjectOnPlane(worldDir, normal);
                if (slopeDir.sqrMagnitude > 1e-4f)
                    slopeDir = slopeDir.normalized * worldDir.magnitude;

                Vector3 current = Vector3.ProjectOnPlane(v, normal);
                _rb.linearVelocity = Vector3.MoveTowards(current, slopeDir * speed, accel * dt);
            }
            else
            {
                Vector3 target = worldDir * speed;
                Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), target, accel * dt);
                _rb.linearVelocity = new Vector3(h.x, v.y, h.z);
            }
        }

        private void ApplyGravity(float dt)
        {
            if (_ground.IsGrounded) return;

            Vector3 v = _rb.linearVelocity;
            float mult = _profile.gravityMultiplier * (v.y < 0f ? _profile.fallGravityMultiplier : 1f);
            v.y += Physics.gravity.y * mult * dt;
            _rb.linearVelocity = v;
        }

        private void ApplyJump()
        {
            if (!CanMove || !JumpAllowed || _jumpBufferTimer <= 0f || _coyoteTimer <= 0f) return;

            float g = Mathf.Abs(Physics.gravity.y) * _profile.gravityMultiplier;
            float jumpVel = Mathf.Sqrt(2f * g * _profile.jumpHeight);

            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVel, _rb.linearVelocity.z);

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _ground.ForceAirborne();
            OnJump?.Invoke();
        }
        
        private void SnapToGround()
        {
            if (!_ground.IsGrounded) return;
            if (_rb.linearVelocity.y > 0.01f && Vector3.Dot(_rb.linearVelocity, _ground.Normal) > 0.01f) return;

            if (_ground.Gap > 0.001f)
                _rb.position -= Vector3.up * _ground.Gap;
        }
        
    }
}