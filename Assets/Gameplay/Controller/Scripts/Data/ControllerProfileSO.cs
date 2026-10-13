using UnityEngine;

namespace Gameplay.Controller
{
    [CreateAssetMenu(menuName = "Profiles/Controller Profile")]
    public class ControllerProfileSO : ScriptableObject
    {
        [Header("Speed")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 7f;
        public float crouchSpeed = 2f;
        public float backwardSpeedMultiplier = 0.7f;
        public float groundAcceleration = 60f;
        public float groundDeceleration = 70f;
        public float airAcceleration = 12f;

        [Header("Jump/Gravity")]
        public float jumpHeight = 1.3f;
        public float gravityMultiplier = 2f;
        public float fallGravityMultiplier = 1.5f; 
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        
        [Header("Crouch")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchTransitionSpeed = 12f;
        
        
        [Header("Stamina")]
        public float maxStamina = 100f;
        public float drainPerSecond = 10f;
        public float regenPerSecond = 10f;
        public float regenDelay = 2f;
    }
}