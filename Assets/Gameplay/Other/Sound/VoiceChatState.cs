using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class VoiceChatState : NetworkBehaviour
{
    public static volatile bool Activated = true;

    private readonly NetworkVariable<bool> _activated = new(true);

    [Header("UI")]
    [SerializeField] private Toggle _toggle;

    public override void OnNetworkSpawn()
    {
        _activated.OnValueChanged += OnActivatedChanged;
        Activated = _activated.Value;

        _toggle.gameObject.SetActive(IsServer);

        if (IsServer)
        {
            _toggle.SetIsOnWithoutNotify(_activated.Value);
            _toggle.onValueChanged.AddListener(SetActivated);
        }
    }

    public override void OnNetworkDespawn()
    {
        _activated.OnValueChanged -= OnActivatedChanged;
        _toggle.onValueChanged.RemoveListener(SetActivated);
    }

    private void SetActivated(bool value)
    {
        if (!IsServer) return;
        _activated.Value = value;
    }

    private void OnActivatedChanged(bool previous, bool current)
    {
        Activated = current;
    }
}