using UnityEngine;
using UnityEngine.InputSystem;

namespace PalaisDesGlaces
{
    /// <summary>
    /// Controller FPS basique : capsule + CharacterController + caméra enfant.
    /// ZQSD/WASD (position physique des touches, donc marche en AZERTY et QWERTY) ou flèches,
    /// souris pour regarder, Shift pour courir, Espace pour sauter,
    /// Échap pour libérer la souris, clic gauche pour la recapturer.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CapsulePlayerController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float walkSpeed = 3.5f;
        public float runSpeed = 6f;
        public float jumpHeight = 0.9f;
        public float gravity = -20f;
        public float mouseSensitivity = 0.12f;
        [Range(10f, 89f)] public float maxPitch = 85f;

        CharacterController _cc;
        float _pitch;
        float _verticalVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        void Start()
        {
            LockCursor(true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

            // Regard
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.Self);
                _pitch = Mathf.Clamp(_pitch - delta.y, -maxPitch, maxPitch);
                if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            // Déplacement
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            Vector3 move = transform.right * x + transform.forward * z;
            if (move.sqrMagnitude > 1f) move.Normalize();
            move *= kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;

            // Gravité / saut
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            if (_cc.isGrounded && kb.spaceKey.wasPressedThisFrame)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _cc.Move(move * Time.deltaTime);
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
