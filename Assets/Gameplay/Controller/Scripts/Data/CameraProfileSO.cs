using UnityEngine;

namespace Gameplay.Controller
{
    [CreateAssetMenu(menuName = "Profiles/Camera Profile")]
    public class CameraProfileSO : ScriptableObject
    {
        [Header("Look")]
        public int maxLookAngle = 90;
        public int minLookAngle = -90;
        
        [Header("Follow")]
        public float verticalSmoothTime = 0.08f;
        public float followReferenceSpeed = 7f;
        public float speedFactorLerp = 12f;

        [Header("Follow - Still")]
        public float horizontalSmoothTimeStill = 0.1f;
        public float maxHorizontalLagStill = 0.15f;

        [Header("Follow - Fast")]
        public float horizontalSmoothTimeFast = 0f;
        public float maxHorizontalLagFast = 0.02f;

        [Header("FOV")]
        public float baseFov = 70f;
        public float crouchFov = 62f;
        public float sprintFov = 78f;
        public float fovLerpSpeed = 8f;

        [Header("Head bob")]
        public bool bobEnabled = true;
        public BobProfile walkBob = new BobProfile { amplitudeY = 0.025f, amplitudeX = 0.015f, roll = 0.3f, frequency = 1.6f };
        public BobProfile sprintBob = new BobProfile { amplitudeY = 0.04f, amplitudeX = 0.025f, roll = 0.6f, frequency = 2.2f };
        public BobProfile crouchBob = new BobProfile { amplitudeY = 0.015f, amplitudeX = 0.01f, roll = 0.2f, frequency = 1.1f };
        public float backwardBobMultiplier = 0.5f;
        public float profileLerpSpeed = 6f;
        public float bobFadeIn = 0.15f;
        public float bobFadeOut = 0.25f;
    }
}