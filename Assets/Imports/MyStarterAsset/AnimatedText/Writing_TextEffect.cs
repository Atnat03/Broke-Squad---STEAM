using System.Collections;
using TMPro;
using UnityEngine;

public class Writing_TextEffect : MonoBehaviour, ITextEffect
{
    private TextMeshProUGUI textMesh;
    private float durationWriting;

    private Coroutine writingCoroutine;
    private bool isWriting;

    public void Init(TextMeshProUGUI textMesh, float speed)
    {
        this.textMesh = textMesh;
        this.durationWriting = speed;
    }

    public void Effect()
    {
        if (writingCoroutine != null)
            StopCoroutine(writingCoroutine);

        writingCoroutine = StartCoroutine(WritingAnimation());
    }

    private IEnumerator WritingAnimation()
    {
        isWriting = true;

        textMesh.ForceMeshUpdate();

        string fullText = textMesh.text;
        textMesh.maxVisibleCharacters = 0;

        int i = 0;

        while (i < fullText.Length)
        {
            i++;
            textMesh.maxVisibleCharacters = i;

            char c = fullText[i - 1];

            float delay = durationWriting;

            if (c == '.' || c == '!' || c == '?')
                delay *= 4f;
            else if (c == ',' || c == ';')
                delay *= 2f;

            yield return new WaitForSeconds(delay);
        }

        isWriting = false;
    }
}