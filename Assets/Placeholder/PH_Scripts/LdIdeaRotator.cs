using System.Collections;
using UnityEngine;

public class LdIdeaRotator : MonoBehaviour
{
    /// <summary>
    /// Il est 3h, y a 0 efforts à mettre là dessus et merci Claude -> Eliott
    /// </summary>
    [Header("State")]
    [Tooltip("While true, the object rotates. Can be set from the Inspector or code too.")]
    public bool isRotating;
 
    [Header("Rotation")]
    [Tooltip("Cruise speed in degrees per second. 180 = 3 degrees per frame at 60 FPS.")]
    [SerializeField] private float degreesPerSecond = 180f;
    [Tooltip("Ease in / ease out time in seconds. Higher = softer start and stop.")]
    [SerializeField] private float easeTime = 0.25f;
    [SerializeField] private Space space = Space.Self;
 
    private float currentSpeed;
    private float speedVelocity; // used by SmoothDamp
 
    /// <summary>Call from a UnityEvent with a bool.</summary>
    public void SetRotating(bool value) => isRotating = value;
 
    private void Update()
    {
        float target = isRotating ? degreesPerSecond : 0f;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, target, ref speedVelocity, easeTime);
 
        // Avoid endless tiny drift when fully stopped.
        if (!isRotating && Mathf.Abs(currentSpeed) < 0.01f)
        {
            currentSpeed = 0f;
            return;
        }
 
        transform.Rotate(0f, currentSpeed * Time.deltaTime, 0f, space);
    }
}
