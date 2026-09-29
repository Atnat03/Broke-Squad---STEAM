using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private float sensX = 0.1f;
        [SerializeField] private float sensY = 0.1f;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerController player; // drag the player here

        private Vector2 _mouseMovement;
        private float _xRotation;
        private float _yRotation;

        private void Awake()
        {
            playerInput.OnMouseMovement += value => _mouseMovement = value;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // mouse delta is already per-frame, so no Time.deltaTime
            _yRotation += _mouseMovement.x * sensX;
            _xRotation -= _mouseMovement.y * sensY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
            player.SetYaw(_yRotation); // rotate the body so movement follows the camera
        }
    }
}