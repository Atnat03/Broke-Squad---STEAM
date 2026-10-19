using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DarkRoomZone : MonoBehaviour
{
    [SerializeField] private RenderingLayerMask blackoutMask;
    [SerializeField] private string pseudoObjectName = "Pseudo";
    [SerializeField] private Color darkColor = Color.black;
    [SerializeField] private Color normalColor = Color.white;

    private readonly Dictionary<Transform, int> playersInside = new Dictionary<Transform, int>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        Debug.Log($"[DarkRoomZone] Prêt sur '{name}'", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DarkRoomZone] ENTER : '{other.name}' (tag '{other.tag}')", other);

        var sw = other.GetComponentInParent<LightLayerSwitcher>();
        if (sw != null) sw.EnterDark(blackoutMask.value);

        var player = GetPlayerRoot(other);
        if (player == null)
        {
            Debug.Log($"[DarkRoomZone] Aucun objet tagué 'Player' trouvé sur '{other.name}' ou ses parents.", other);
            return;
        }
        Debug.Log($"[DarkRoomZone] Joueur trouvé : '{player.name}'", player);

        playersInside.TryGetValue(player, out int count);
        playersInside[player] = count + 1;
        Debug.Log($"[DarkRoomZone] '{player.name}' compteur = {count + 1}", player);

        if (count == 0) SetPseudoColor(player, darkColor);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[DarkRoomZone] EXIT : '{other.name}'", other);

        var sw = other.GetComponentInParent<LightLayerSwitcher>();
        if (sw != null) sw.ExitDark();

        var player = GetPlayerRoot(other);
        if (player == null) return;

        if (!playersInside.TryGetValue(player, out int count))
        {
            Debug.LogWarning($"[DarkRoomZone] '{player.name}' sort mais n'était pas enregistré comme entré.", player);
            return;
        }

        count--;
        Debug.Log($"[DarkRoomZone] '{player.name}' compteur = {count}", player);

        if (count <= 0)
        {
            playersInside.Remove(player);
            SetPseudoColor(player, normalColor);
        }
        else
        {
            playersInside[player] = count;
        }
    }

    [SerializeField] private string playerLayerName = "Player";

// Remonte jusqu'à l'objet le plus haut qui est sur le layer Player
    private Transform GetPlayerRoot(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        Transform found = null;
        Transform t = other.transform;

        while (t != null)
        {
            if (t.gameObject.layer == playerLayer) found = t;
            t = t.parent;
        }
        return found;
    }

    private void SetPseudoColor(Transform player, Color color)
    {
        Transform pseudo = FindChildRecursive(player, pseudoObjectName);
        if (pseudo == null)
        {
            Debug.LogError($"[DarkRoomZone] Aucun enfant nommé '{pseudoObjectName}' sous '{player.name}'. Vérifie le nom exact (majuscules, espaces) et qu'il est bien sous l'objet tagué Player.", player);
            return;
        }
        Debug.Log($"[DarkRoomZone] Objet '{pseudoObjectName}' trouvé : '{GetPath(pseudo)}'", pseudo);

        var ugui = pseudo.GetComponentInChildren<TextMeshProUGUI>(true);
        if (ugui != null)
        {
            ugui.color = color;
            Debug.Log($"[DarkRoomZone] Couleur de '{ugui.name}' → {color}", ugui);
            return;
        }

        var anyTmp = pseudo.GetComponentInChildren<TMP_Text>(true);
        if (anyTmp != null)
        {
            Debug.LogWarning($"[DarkRoomZone] Pas de TextMeshProUGUI, mais un {anyTmp.GetType().Name} sur '{anyTmp.name}'. Couleur appliquée quand même.", anyTmp);
            anyTmp.color = color;
            return;
        }

        Debug.LogError($"[DarkRoomZone] Aucun composant TextMeshPro sur '{pseudo.name}' ni ses enfants.", pseudo);
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.parent != null || true ? t.name + "/" + path : path; }
        return path;
    }
}