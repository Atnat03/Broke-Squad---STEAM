using System.Collections;
using UnityEngine;

/// <summary>
/// Objet cliquable du bureau : la caméra se déplace vers viewPoint et l'objet joue une petite animation.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DeskInteractable : MonoBehaviour
{
    [Tooltip("Point de vue de la caméra quand cet objet est sélectionné (déplace/tourne ce Transform pour régler le cadrage).")]
    public Transform viewPoint;

    [Header("Animation de réaction")]
    public float duration = 0.5f;
    public float hopHeight = 0.03f;
    [Range(0f, 1f)] public float punchScale = 0.2f;
    public float wiggleAngle = 10f;
    public int wiggleCount = 3;

    Vector3 basePos, baseScale;
    Quaternion baseRot;
    Coroutine anim;

    void Awake()
    {
        basePos = transform.localPosition;
        baseScale = transform.localScale;
        baseRot = transform.localRotation;
    }

    public void PlayReaction()
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(React());
    }

    IEnumerator React()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float bump = Mathf.Sin(k * Mathf.PI);          // 0 -> 1 -> 0
            float decay = 1f - k;
            transform.localPosition = basePos + Vector3.up * (bump * hopHeight);
            transform.localScale = baseScale * (1f + bump * punchScale * decay);
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(k * Mathf.PI * 2f * wiggleCount) * wiggleAngle * decay, 0f);
            yield return null;
        }
        transform.localPosition = basePos;
        transform.localScale = baseScale;
        transform.localRotation = baseRot;
        anim = null;
    }
}
