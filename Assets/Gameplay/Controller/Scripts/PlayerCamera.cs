using UnityEngine;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        
        [SerializeField] private CameraProfileSO profile;
        public bool autoUpdateProfileValue = true;
        
        [Header("References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform eyeTarget;

        [Header("Look")]
        [SerializeField] private float sensX = 0.1f;
        [SerializeField] private float sensY = 0.1f;
        private int _maxLookAngle = 90;
        private int _minLookAngle = -90;

        [Header("Follow")]
        private float _verticalSmoothTime = 0.08f;
        private float _followReferenceSpeed = 7f;
        private float _speedFactorLerp = 12f;

        [Header("Follow - Still")]
         private float _horizontalSmoothTimeStill = 0.1f;
         private float _maxHorizontalLagStill = 0.15f;

        [Header("Follow - Fast")]
         private float _horizontalSmoothTimeFast = 0f;
         private float _maxHorizontalLagFast = 0.02f;

        [Header("FOV")]
         private float _baseFov = 70f;
         private float _crouchFov = 62f;
         private float _sprintFov = 78f;
         private float _fovLerpSpeed = 8f;

        [Header("Head bob")]
        private bool _bobEnabled = true;
        private BobProfile _walkBob = new BobProfile { amplitudeY = 0.025f, amplitudeX = 0.015f, roll = 0.3f, frequency = 1.6f };
        private BobProfile _sprintBob = new BobProfile { amplitudeY = 0.04f, amplitudeX = 0.025f, roll = 0.6f, frequency = 2.2f };
        private BobProfile _crouchBob = new BobProfile { amplitudeY = 0.015f, amplitudeX = 0.01f, roll = 0.2f, frequency = 1.1f };
        private float _backwardBobMultiplier = 0f;
        private float _profileLerpSpeed = 6f;
        private float _bobFadeIn = 0.15f;
        private float _bobFadeOut = 0.25f;

        private Camera _cam;
        private Vector2 _mouseMovement;
        private float _xRotation;
        private float _yRotation;

        private Vector3 _smoothPos;
        private Vector3 _velH;
        private float _velY;
        private float _speedFactor;

        private BobProfile _bob;
        private float _bobPhase;
        private float _bobWeight;
        private float _backwardFactor;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            playerInput.OnMouseMovement += value => _mouseMovement = value;
            _bob = _walkBob;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _cam.fieldOfView = _baseFov;
            _smoothPos = eyeTarget.position;
        }

        private void Update()
        {
            if(autoUpdateProfileValue) GetDataFromProfile();
            
            _yRotation += _mouseMovement.x * sensX;
            _xRotation -= _mouseMovement.y * sensY;
            _xRotation = Mathf.Clamp(_xRotation, _minLookAngle, _maxLookAngle);

            player.SetYaw(_yRotation);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            UpdateFov(dt);
            UpdateFollow(dt);
            Vector3 bobOffset = UpdateBob(dt, out float bobRoll);

            Quaternion look = Quaternion.Euler(_xRotation, _yRotation, 0f);
            transform.rotation = look * Quaternion.Euler(0f, 0f, bobRoll);

            Quaternion yawOnly = Quaternion.Euler(0f, _yRotation, 0f);
            transform.position = _smoothPos + yawOnly * bobOffset;
        }

        private void UpdateFov(float dt)
        {
            float target = player.IsCrouching ? _crouchFov
                : player.IsSprinting ? _sprintFov
                : _baseFov;

            float alpha = 1f - Mathf.Exp(-_fovLerpSpeed * dt);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, alpha);
        }

        private void UpdateFollow(float dt)
        {
            Vector3 target = eyeTarget.position;

            float rawFactor = Mathf.Clamp01(player.HorizontalVelocity.magnitude / _followReferenceSpeed);
            _speedFactor = Mathf.Lerp(_speedFactor, rawFactor, 1f - Mathf.Exp(-_speedFactorLerp * dt));

            float smoothTime = Mathf.Lerp(_horizontalSmoothTimeStill, _horizontalSmoothTimeFast, _speedFactor);
            float maxLag = Mathf.Lerp(_maxHorizontalLagStill, _maxHorizontalLagFast, _speedFactor);

            Vector3 h = new Vector3(_smoothPos.x, 0f, _smoothPos.z);
            Vector3 hTarget = new Vector3(target.x, 0f, target.z);

            h = smoothTime > 0.0001f
                ? Vector3.SmoothDamp(h, hTarget, ref _velH, smoothTime)
                : hTarget;

            Vector3 offset = h - hTarget;
            if (offset.sqrMagnitude > maxLag * maxLag)
                h = hTarget + offset.normalized * maxLag;

            float y = _verticalSmoothTime > 0.0001f
                ? Mathf.SmoothDamp(_smoothPos.y, target.y, ref _velY, _verticalSmoothTime)
                : target.y;

            _smoothPos = new Vector3(h.x, y, h.z);
        }

        private Vector3 UpdateBob(float dt, out float roll)
        {
            roll = 0f;
            if (!_bobEnabled) return Vector3.zero;

            Vector3 horizVel = player.HorizontalVelocity;
            bool moving = player.IsGrounded && horizVel.magnitude > 0.1f;

            BobProfile targetProfile = player.IsCrouching ? _crouchBob
                : player.IsSprinting ? _sprintBob
                : _walkBob;
            _bob = BobProfile.Lerp(_bob, targetProfile, 1f - Mathf.Exp(-_profileLerpSpeed * dt));
            
            float targetBackward = moving ? Mathf.Clamp01(-player.ForwardDot) : 0f; 
            _backwardFactor = Mathf.Lerp(_backwardFactor, targetBackward, 1f - Mathf.Exp(-_profileLerpSpeed * dt));
            
            float backwardScale = Mathf.Lerp(1f, _backwardBobMultiplier, _backwardFactor);

            float fade = Mathf.Max(0.01f, moving ? _bobFadeIn : _bobFadeOut);
            _bobWeight = Mathf.MoveTowards(_bobWeight, moving ? 1f : 0f, dt / fade);

            _bobPhase = (_bobPhase + _bob.frequency * dt * Mathf.PI * 2f) % (Mathf.PI * 2f);

            float weight = _bobWeight * backwardScale;

            float x = Mathf.Sin(_bobPhase) * _bob.amplitudeX * weight;
            float y = Mathf.Sin(_bobPhase * 2f) * _bob.amplitudeY * weight;
            roll = Mathf.Sin(_bobPhase) * _bob.roll * weight;

            return new Vector3(x, y, 0f);
        }


        void GetDataFromProfile()
        {
            if(profile == null) return;
            _verticalSmoothTime = profile.verticalSmoothTime;
            _followReferenceSpeed = profile.followReferenceSpeed;
            _speedFactorLerp = profile.speedFactorLerp;
            _horizontalSmoothTimeStill = profile.horizontalSmoothTimeStill;
            _maxHorizontalLagStill = profile.maxHorizontalLagStill;
            _horizontalSmoothTimeFast = profile.horizontalSmoothTimeFast;
            _maxHorizontalLagFast = profile.maxHorizontalLagFast;
            _baseFov = profile.baseFov;
            _crouchFov = profile.crouchFov;
            _sprintFov = profile.sprintFov;
            _fovLerpSpeed = profile.fovLerpSpeed;
            _bobEnabled = profile.bobEnabled;
            _walkBob = profile.walkBob;
            _sprintBob = profile.sprintBob;
            _crouchBob = profile.crouchBob;
            _fovLerpSpeed = profile.fovLerpSpeed;
            _bobFadeIn = profile.bobFadeIn;
            _bobFadeOut = profile.bobFadeOut;
            _profileLerpSpeed = profile.profileLerpSpeed;
            _backwardBobMultiplier = profile.backwardBobMultiplier;
            _maxLookAngle = profile.maxLookAngle;
            _minLookAngle = profile.minLookAngle;

        }
    }
}

[System.Serializable]
public struct BobProfile
{
    public float amplitudeY;
    public float amplitudeX;
    public float roll;
    public float frequency;

    public static BobProfile Lerp(BobProfile a, BobProfile b, float t) => new BobProfile
    {
        amplitudeY = Mathf.Lerp(a.amplitudeY, b.amplitudeY, t),
        amplitudeX = Mathf.Lerp(a.amplitudeX, b.amplitudeX, t),
        roll = Mathf.Lerp(a.roll, b.roll, t),
        frequency = Mathf.Lerp(a.frequency, b.frequency, t),
    };
}