using UnityEngine;
using TMPro;

public class Shaking_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private bool IsLoop = true;
    
    private float intensity = 5f;
    private float duration = 0.3f;

    private Vector3 basePosition;
    private bool isShaking;
    private float timer;

    public void Init(TextMeshProUGUI textMesh, bool loop, float intensity, float duration)
    {
        this.textMesh = textMesh;

        this.intensity = intensity;
        this.duration = duration;

        IsLoop = loop;
        
        basePosition = textMesh.rectTransform.anchoredPosition;
    }

    public void Effect()
    {
        if (!isShaking)
        {
            basePosition = textMesh.rectTransform.anchoredPosition;
        }

        isShaking = true;
        timer = duration;
    }

    private void Update()
    {
        if (!isShaking || textMesh == null) return;

        timer -= Time.deltaTime;

        Vector2 offset = Random.insideUnitCircle * intensity;
        textMesh.rectTransform.anchoredPosition = basePosition + (Vector3)offset;

        if (timer <= 0f)
        {
            isShaking = false;
            textMesh.rectTransform.anchoredPosition = basePosition;
            
            if(IsLoop)
                timer = duration;
        }
    }
}