using TMPro;
using UnityEngine;

public class MovingGradient_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private Color colorA = Color.cyan;
    private Color colorB = Color.magenta;
    private float speed = 1f;

    private bool isLoop = false;
    private bool isPlaying = false;

    private float duration = 2f;
    private float timer = 0f;

    private float offset;

    // 👉 longueur texte normalisée (calculée)
    private float textWidth;

    public void Init(TextMeshProUGUI textMesh,
        Color a, Color b, float s, bool loop, float duration = 2f)
    {
        this.textMesh = textMesh;
        colorA = a;
        colorB = b;
        speed = s;
        isLoop = loop;
        this.duration = duration;
    }

    public void Effect()
    {
        offset = 0f;
        timer = 0f;
        isPlaying = true;

        CacheTextWidth();
    }

    private void CacheTextWidth()
    {
        textMesh.ForceMeshUpdate();

        var textInfo = textMesh.textInfo;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            float x = (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        textWidth = maxX - minX;
    }

    private void Update()
    {
        if (textMesh == null || !isPlaying) return;

        textMesh.ForceMeshUpdate();

        timer += Time.deltaTime;

        // 👉 durée auto en ONE SHOT
        if (!isLoop)
        {
            float travelTime = textWidth / speed;
            if (timer >= travelTime)
            {
                isPlaying = false;
                return;
            }
        }

        offset += Time.deltaTime * speed;

        var textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            float charX = (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f;

            float normalizedX = charX / textWidth;

            float wave = offset - normalizedX;

            float t = Mathf.PingPong(wave, 1f);

            Color col = Color.Lerp(colorA, colorB, t);

            colors[vertexIndex + 0] = col;
            colors[vertexIndex + 1] = col;
            colors[vertexIndex + 2] = col;
            colors[vertexIndex + 3] = col;
        }

        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}