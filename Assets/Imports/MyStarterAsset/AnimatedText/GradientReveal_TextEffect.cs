using System.Collections;
using TMPro;
using UnityEngine;

public class GradientReveal_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;

    private Color startColor = Color.white;
    private Color endColor = Color.red;

    private float revealSpeed = 30f;

    private Coroutine routine;

    public void Init(TextMeshProUGUI textMesh, Color s, Color e, float speed)
    {
        this.textMesh = textMesh;

        startColor = s;
        endColor = e;
        revealSpeed = speed;
    }

    public void Effect()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        Debug.Log("Reveal");
        
        textMesh.ForceMeshUpdate();

        var textInfo = textMesh.textInfo;
        int charCount = textInfo.characterCount;

        float progress = 0f;
        
        progress += revealSpeed * Time.deltaTime;
        
        while (progress < charCount)
        {
            progress += revealSpeed * Time.deltaTime;

            int visibleCount = Mathf.FloorToInt(progress);
            visibleCount = Mathf.Clamp(visibleCount, 0, charCount);

            textMesh.ForceMeshUpdate();

            for (int i = 0; i < visibleCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                int materialIndex = charInfo.materialReferenceIndex;

                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                float t = (float)i / charCount;
                Color color = Color.Lerp(startColor, endColor, t);

                colors[vertexIndex + 0] = color;
                colors[vertexIndex + 1] = color;
                colors[vertexIndex + 2] = color;
                colors[vertexIndex + 3] = color;
            }

            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }
    }
}