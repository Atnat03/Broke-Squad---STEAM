using TMPro;
using UnityEngine;

public class RainbowWave_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private float speed = 2f;
    private float hueOffset = 0.1f;

    private bool active;

    public void Init(TextMeshProUGUI textMesh,
        float speed, float hueOffset)
    {
        this.textMesh = textMesh;
        
        this.speed = speed;
        this.hueOffset = hueOffset;
    }

    public void Effect()
    {
        active = true;
    }

    private void Update()
    {
        if (!active || textMesh == null) return;

        textMesh.ForceMeshUpdate();

        var textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            float hue = Mathf.Repeat(Time.time * speed + i * hueOffset, 1f);
            Color color = Color.HSVToRGB(hue, 1f, 1f);

            colors[vertexIndex + 0] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;
        }

        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}