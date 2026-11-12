using System;
using System.Collections;
using Bus;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraShake : NetworkBusListener
{
    private Coroutine _currentShake;
    private Vector3 _initialLocalPos;
    
    public void Awake()
    {
        _initialLocalPos = transform.localPosition;

        ListenToEvent<OnCameraShakeEvent>(Shake);
    }

    void Shake(OnCameraShakeEvent data)
    {
        if (data.PlayerClientId != OwnerClientId) return;
        
        if (_currentShake != null)
            StopCoroutine(_currentShake);

        _currentShake = StartCoroutine(Shaking(data.Duration, data.Magnitude));
    }

    IEnumerator Shaking(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = _initialLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _initialLocalPos;
        _currentShake = null;
    }
}

public struct OnCameraShakeEvent
{
    public ulong PlayerClientId;
    public float Duration;
    public float Magnitude;
}