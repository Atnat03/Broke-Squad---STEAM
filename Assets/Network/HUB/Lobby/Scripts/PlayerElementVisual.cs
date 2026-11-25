using Bus;
using MyPrint;
using Network.Connections;
using TMPro;
using UnityEngine;

namespace Network.HUB
{
    public class PlayerElementVisual : MonoBusListener
    {
        [SerializeField] private TextMeshProUGUI _namePlayerText;
        [SerializeField] private SkinnedMeshRenderer[] _meshRendererPlayer;
        [SerializeField] private TextMeshProUGUI _readyText;

        void Start()
        {
            _readyText.gameObject.SetActive(false);
        }
        
        public void SetData(PlayerDataLobby data)
        {
            _namePlayerText.text = data.Name;

            foreach (var m in  _meshRendererPlayer)
            {
                m.material.color = PlayerLocalData.instance.PossibleColor[data.ColorID];
            }

            _readyText.gameObject.SetActive(data.IsReady);
        }

        public void SetLocalColor(int colorId)
        {
            foreach (var m in  _meshRendererPlayer)
            {
                m.material.color = PlayerLocalData.instance.PossibleColor[colorId];
            }
        }

        public void SetLocalIsReady(bool isReady)
        {
            ABPrint.Print("ready");
            _readyText.gameObject.SetActive(isReady);
        }
    }

    public class PlayerDataLobby
    {
        public string ID;
        public int ColorID;
        public string Name;
        public bool IsReady;
    }
}