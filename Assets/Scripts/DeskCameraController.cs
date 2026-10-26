using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Caméra du bureau : plan cinématique d'intro, focus sur les objets cliqués, retour avec Échap.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DeskCameraController : MonoBehaviour
{
    [Header("Points de vue")]
    [Tooltip("Point de vue principal (retour avec Échap).")]
    public Transform initialView;
    [Tooltip("Position de départ du plan d'intro (laisser vide pour désactiver l'intro).")]
    public Transform introStart;

    [Header("Timings")]
    public float introDelay = 0.3f;
    public float introDuration = 4f;
    public float focusDuration = 0.9f;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sélection")]
    public LayerMask clickMask = ~0;
    public float maxClickDistance = 20f;

    Camera cam;
    Coroutine move;
    bool busyIntro;
    DeskInteractable current;

    void Awake() { cam = GetComponent<Camera>(); }

    void Start()
    {
        if (introStart != null && initialView != null)
        {
            transform.SetPositionAndRotation(introStart.position, introStart.rotation);
            StartCoroutine(Intro());
        }
        else if (initialView != null)
        {
            transform.SetPositionAndRotation(initialView.position, initialView.rotation);
        }
    }

    IEnumerator Intro()
    {
        busyIntro = true;
        yield return new WaitForSeconds(introDelay);
        yield return MoveRoutine(initialView, introDuration);
        busyIntro = false;
    }

    void Update()
    {
        if (busyIntro) return;

        if (ClickPressed())
        {
            Ray ray = cam.ScreenPointToRay(PointerPosition());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxClickDistance, clickMask))
            {
                var item = hit.collider.GetComponentInParent<DeskInteractable>();
                if (item != null) Focus(item);
            }
        }

        if (EscapePressed() && current != null)
        {
            current = null;
            GoTo(initialView, focusDuration);
        }
    }

    public void Focus(DeskInteractable item)
    {
        item.PlayReaction();
        if (item.viewPoint == null) return;
        current = item;
        GoTo(item.viewPoint, focusDuration);
    }

    void GoTo(Transform target, float duration)
    {
        if (target == null) return;
        if (move != null) StopCoroutine(move);
        move = StartCoroutine(MoveRoutine(target, duration));
    }

    IEnumerator MoveRoutine(Transform target, float duration)
    {
        Vector3 p0 = transform.position;
        Quaternion r0 = transform.rotation;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = easing.Evaluate(Mathf.Clamp01(t / duration));
            // la cible est relue chaque frame : on peut régler le Transform en Play mode
            transform.SetPositionAndRotation(Vector3.LerpUnclamped(p0, target.position, k),
                                             Quaternion.SlerpUnclamped(r0, target.rotation, k));
            yield return null;
        }
        transform.SetPositionAndRotation(target.position, target.rotation);
        move = null;
    }

    // ---------- Entrées (nouvel Input System ou ancien) ----------
    bool ClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
