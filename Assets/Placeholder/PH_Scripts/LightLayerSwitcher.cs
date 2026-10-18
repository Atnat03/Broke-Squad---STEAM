using System.Collections.Generic;
using UnityEngine;

public class LightLayerSwitcher : MonoBehaviour
{
    private readonly Dictionary<MeshRenderer, uint> originalMasks = new Dictionary<MeshRenderer, uint>();
    private int darkZoneCount;

    public void EnterDark(uint blackoutMask)
    {
        darkZoneCount++;
        if (darkZoneCount != 1) return;

        originalMasks.Clear();
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
        {
            originalMasks[r] = r.renderingLayerMask;
            r.renderingLayerMask = blackoutMask;
        }
    }

    public void ExitDark()
    {
        darkZoneCount = Mathf.Max(0, darkZoneCount - 1);
        if (darkZoneCount != 0) return;

        foreach (var pair in originalMasks)
        {
            if (pair.Key != null)
                pair.Key.renderingLayerMask = pair.Value;
        }
        originalMasks.Clear();
    }
}