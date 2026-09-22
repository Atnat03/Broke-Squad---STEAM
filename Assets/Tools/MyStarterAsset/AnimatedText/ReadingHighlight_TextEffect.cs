using TMPro;
using UnityEngine;

public class ReadingHighlight_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private Color baseColor = Color.white;
    private Color highlightColor = Color.yellow;
    private float speed = 20f;

    private float progress;

    public void Init(TextMeshProUGUI textMesh,
        Color b, Color h, float s)
    {
        this.textMesh = textMesh;
        
        baseColor = b;
        highlightColor = h;
        speed = s;
    }

    public void Effect()
    {
        progress = 0f;
    }

    private void Update()
    {
        if (textMesh == null) return;

        textMesh.ForceMeshUpdate();

        var textInfo = textMesh.textInfo;
        int charCount = textInfo.characterCount;

        progress += speed * Time.deltaTime;

        int current = Mathf.FloorToInt(progress) % Mathf.Max(1, charCount);

        for (int i = 0; i < charCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            Color col = (i == current) ? highlightColor : baseColor;

            colors[vertexIndex + 0] = col;
            colors[vertexIndex + 1] = col;
            colors[vertexIndex + 2] = col;
            colors[vertexIndex + 3] = col;
        }

        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}