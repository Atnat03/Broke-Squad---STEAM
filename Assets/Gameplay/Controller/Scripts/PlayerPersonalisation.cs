using Gameplay.PlayerData;
using Network.Connections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerPersonalisation : NetworkBehaviour
    {
        [SerializeField] private MeshRenderer[] meshRenderer;
        [SerializeField] private TextMeshProUGUI pseudoText;

        private int _syncedColor;
        private string _syncedName = "";

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                foreach (var mesh in meshRenderer) mesh.enabled = false;
                pseudoText.gameObject.SetActive(false);
                SendPersonalisation();
            }
            else
            {
                RequestPersonalisationRpc();
            }
        }

        public void SendPersonalisation()
        {
            if (!IsOwner) return;
            SubmitPersonalisationRpc(PlayerLocalData.instance.PlayerColor, PlayerLocalData.instance.PlayerName);
        }

        [Rpc(SendTo.Server)]
        private void SubmitPersonalisationRpc(int colorId, string playerName)
        {
            _syncedColor = colorId;
            _syncedName = playerName;
            ReplicatePersonalisationRpc(colorId, playerName);
        }

        [Rpc(SendTo.Server)]
        private void RequestPersonalisationRpc()
        {
            ReplicatePersonalisationRpc(_syncedColor, _syncedName);
        }

        [Rpc(SendTo.Everyone)]
        private void ReplicatePersonalisationRpc(int colorId, string playerName)
        {
            if (PlayerLocalData.instance == null) return;

            meshRenderer[0].material.color = PlayerLocalData.instance.PossibleColor[colorId];
            pseudoText.text = playerName;
        }
    }
}