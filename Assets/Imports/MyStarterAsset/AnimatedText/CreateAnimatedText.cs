#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public class CreateAnimatedText
{
    [MenuItem("GameObject/UI (Canvas)/Animated Text")]
    public static void CreateCustomObject()
    {
        GameObject obj = new GameObject("new Animated Text");
        obj.AddComponent<TextMeshProUGUI>();
        obj.AddComponent<AnimatedText>();
    }
}

#endif
