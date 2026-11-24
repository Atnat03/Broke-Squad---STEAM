using Gameplay.Controller;
using UnityEngine;

public class CameraFreeRotateLook : MonoBehaviour
{
    public const string Key = "MouseSensitivity";
    
    [SerializeField] PlayerInput _playerInput;

    private float _sensitivity;
    
    void OnEnable()
    {
        _sensitivity = PlayerPrefs.GetFloat(Key);
        
        _playerInput.OnMouseMovement += UpdateRotation;
    }
    
    void OnDisable()
    {
        _playerInput.OnMouseMovement -= UpdateRotation;
    }


    private void UpdateRotation(Vector2 input)
    {
        float rotationY = input.x * _sensitivity;

        transform.Rotate(0f, rotationY, 0f, Space.Self);
    }
}
