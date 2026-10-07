using TMPro;
using UnityEngine;

public class ColorPulse_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private Color colorA = Color.white;
    private Color colorB = Color.cyan;
    private float speed = 2f;

    private bool active;

    public void Init(TextMeshProUGUI textMesh,
        Color a, Color b, float s)
    {
        this.textMesh = textMesh;
        
        colorA = a;
        colorB = b;
        speed = s;
    }

    public void Effect()
    {
        active = true;
    }

    private void Update()
    {
        if (!active || textMesh == null) return;

        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        textMesh.color = Color.Lerp(colorA, colorB, t);
    }
}