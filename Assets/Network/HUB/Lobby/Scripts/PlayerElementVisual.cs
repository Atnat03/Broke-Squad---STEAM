using Bus;
using Network.Connections;
using TMPro;
using UnityEngine;

namespace Network.HUB
{
    public class PlayerElementVisual : MonoBusListener
    {
        [SerializeField] private TextMeshProUGUI _namePlayerText;
        [SerializeField] private MeshRenderer _meshRendererPlayer;
        [SerializeField] private TextMeshProUGUI _readyText;

        void Start()
        {
            _readyText.gameObject.SetActive(false);
        }
        
        public void SetData(PlayerDataLobby data)
        {
            _namePlayerText.text = data.Name;
            
            _meshRendererPlayer.material.color = PlayerLocalData.instance.PossibleColor[data.ColorID];

            _readyText.gameObject.SetActive(data.IsReady);
        }
    }

    public class PlayerDataLobby
    {
        public int ColorID;
        public string Name;
        public bool IsReady;
    }
}