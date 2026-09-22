using TMPro;
using UnityEngine;

public class FlashLoopColor_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private Color flashColor = Color.yellow;
    private float duration = 0.2f;

    private Color originalColor;
    private float timer;
    private bool active;

    public void Init(TextMeshProUGUI textMesh,Color flashColor, float duration)
    {
        this.textMesh = textMesh;
        originalColor = textMesh.color;
        
        this.flashColor = flashColor;
        this.duration = duration;
    }

    public void Effect()
    {
        timer = duration;
        active = true;
    }

    private void Update()
    {
        if (!active || textMesh == null) return;

        timer -= Time.deltaTime;

        textMesh.color = flashColor;

        if (timer <= 0f)
        {
            textMesh.color = originalColor;
            active = false;
        }
    }
}