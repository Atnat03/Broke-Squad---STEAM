using UnityEngine;

namespace Gameplay.Controller
{
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
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

        [Header("References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform eyeTarget;

        [Header("Look")]
        [SerializeField] private float sensX = 0.1f;
        [SerializeField] private float sensY = 0.1f;

        [Header("Follow")]
        [SerializeField] private float verticalSmoothTime = 0.08f;
        [SerializeField] private float followReferenceSpeed = 7f;
        [SerializeField] private float speedFactorLerp = 12f;

        [Header("Follow - Still")]
        [SerializeField] private float horizontalSmoothTimeStill = 0.1f;
        [SerializeField] private float maxHorizontalLagStill = 0.15f;

        [Header("Follow - Fast")]
        [SerializeField] private float horizontalSmoothTimeFast = 0f;
        [SerializeField] private float maxHorizontalLagFast = 0.02f;

        [Header("FOV")]
        [SerializeField] private float baseFov = 70f;
        [SerializeField] private float crouchFov = 62f;
        [SerializeField] private float sprintFov = 78f;
        [SerializeField] private float fovLerpSpeed = 8f;

        [Header("Head bob")]
        [SerializeField] private bool bobEnabled = true;
        [SerializeField] private BobProfile walkBob = new BobProfile { amplitudeY = 0.025f, amplitudeX = 0.015f, roll = 0.3f, frequency = 1.6f };
        [SerializeField] private BobProfile sprintBob = new BobProfile { amplitudeY = 0.04f, amplitudeX = 0.025f, roll = 0.6f, frequency = 2.2f };
        [SerializeField] private BobProfile crouchBob = new BobProfile { amplitudeY = 0.015f, amplitudeX = 0.01f, roll = 0.2f, frequency = 1.1f };
        [SerializeField] private float profileLerpSpeed = 6f;
        [SerializeField] private float bobFadeIn = 0.15f;
        [SerializeField] private float bobFadeOut = 0.25f;

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

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            playerInput.OnMouseMovement += value => _mouseMovement = value;
            _bob = walkBob;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _cam.fieldOfView = baseFov;
            _smoothPos = eyeTarget.position;
        }

        private void Update()
        {
            _yRotation += _mouseMovement.x * sensX;
            _xRotation -= _mouseMovement.y * sensY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

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
            float target = player.IsCrouching ? crouchFov
                : player.IsSprinting ? sprintFov
                : baseFov;

            float alpha = 1f - Mathf.Exp(-fovLerpSpeed * dt);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, alpha);
        }

        private void UpdateFollow(float dt)
        {
            Vector3 target = eyeTarget.position;

            float rawFactor = Mathf.Clamp01(player.HorizontalVelocity.magnitude / followReferenceSpeed);
            _speedFactor = Mathf.Lerp(_speedFactor, rawFactor, 1f - Mathf.Exp(-speedFactorLerp * dt));

            float smoothTime = Mathf.Lerp(horizontalSmoothTimeStill, horizontalSmoothTimeFast, _speedFactor);
            float maxLag = Mathf.Lerp(maxHorizontalLagStill, maxHorizontalLagFast, _speedFactor);

            Vector3 h = new Vector3(_smoothPos.x, 0f, _smoothPos.z);
            Vector3 hTarget = new Vector3(target.x, 0f, target.z);

            h = smoothTime > 0.0001f
                ? Vector3.SmoothDamp(h, hTarget, ref _velH, smoothTime)
                : hTarget;

            Vector3 offset = h - hTarget;
            if (offset.sqrMagnitude > maxLag * maxLag)
                h = hTarget + offset.normalized * maxLag;

            float y = verticalSmoothTime > 0.0001f
                ? Mathf.SmoothDamp(_smoothPos.y, target.y, ref _velY, verticalSmoothTime)
                : target.y;

            _smoothPos = new Vector3(h.x, y, h.z);
        }

        private Vector3 UpdateBob(float dt, out float roll)
        {
            roll = 0f;
            if (!bobEnabled) return Vector3.zero;

            bool moving = player.IsGrounded && player.HorizontalVelocity.magnitude > 0.1f;

            BobProfile targetProfile = player.IsCrouching ? crouchBob
                : player.IsSprinting ? sprintBob
                : walkBob;
            _bob = BobProfile.Lerp(_bob, targetProfile, 1f - Mathf.Exp(-profileLerpSpeed * dt));

            float fade = Mathf.Max(0.01f, moving ? bobFadeIn : bobFadeOut);
            _bobWeight = Mathf.MoveTowards(_bobWeight, moving ? 1f : 0f, dt / fade);

            _bobPhase = (_bobPhase + _bob.frequency * dt * Mathf.PI * 2f) % (Mathf.PI * 2f);

            float x = Mathf.Sin(_bobPhase) * _bob.amplitudeX * _bobWeight;
            float y = Mathf.Sin(_bobPhase * 2f) * _bob.amplitudeY * _bobWeight;
            roll = Mathf.Sin(_bobPhase) * _bob.roll * _bobWeight;

            return new Vector3(x, y, 0f);
        }
    }
}