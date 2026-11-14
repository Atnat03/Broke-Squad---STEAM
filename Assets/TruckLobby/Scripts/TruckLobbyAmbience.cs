using UnityEngine;

// Ambiance du lobby camion : gyrophare, hologramme, LEDs qui clignotent, pads qui pulsent.
public class TruckLobbyAmbience : MonoBehaviour
{
    public Transform beacon;
    public float beaconSpeed = 220f;
    public Transform holo;
    public float holoSpeed = 20f;
    public Light holoLight;
    public Light[] padLights;
    public Renderer[] blinkingLeds;

    float[] ledOffsets;

    void Start()
    {
        if (blinkingLeds != null)
        {
            ledOffsets = new float[blinkingLeds.Length];
            for (int i = 0; i < ledOffsets.Length; i++) ledOffsets[i] = Random.Range(0f, 10f);
        }
    }

    void Update()
    {
        float t = Time.time;
        if (beacon) beacon.Rotate(0f, beaconSpeed * Time.deltaTime, 0f, Space.World);
        if (holo)
        {
            holo.Rotate(0f, holoSpeed * Time.deltaTime, 0f, Space.World);
            holo.localPosition = new Vector3(holo.localPosition.x, 0.9f + Mathf.Sin(t * 1.3f) * 0.02f, holo.localPosition.z);
        }
        if (holoLight) holoLight.intensity = 2f + Mathf.PerlinNoise(t * 3f, 0f) * 0.8f;
        if (padLights != null)
            for (int i = 0; i < padLights.Length; i++)
                if (padLights[i]) padLights[i].intensity = 1.2f + Mathf.Sin(t * 2f + i) * 0.4f;
        if (blinkingLeds != null && ledOffsets != null)
            for (int i = 0; i < blinkingLeds.Length; i++)
                if (blinkingLeds[i]) blinkingLeds[i].enabled = Mathf.PerlinNoise(t * 2.5f + ledOffsets[i], i) > 0.35f;
    }
}
