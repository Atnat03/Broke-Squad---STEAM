using UnityEngine;
using TMPro;

public class Floating_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    
    private float amplitude = 5f;
    private float speed = 2f;

    private Vector3 basePosition;
    private bool isActive;

    public void Init(TextMeshProUGUI textMesh, float speedValue, float amplitudeValue)
    {
        this.textMesh = textMesh;
        this.speed = speedValue > 0 ? speedValue : this.speed;

        basePosition = textMesh.rectTransform.anchoredPosition;
        amplitude = amplitudeValue;
    }

    public void Effect()
    {
        isActive = true;
    }

    private void Update()
    {
        if (!isActive || textMesh == null) return;

        float y = Mathf.Sin(Time.time * speed) * amplitude;

        textMesh.rectTransform.anchoredPosition = basePosition + new Vector3(0, y, 0);
    }
}