using Bus;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.PlayerData
{
    public class UITest : NetworkBusListener
    {
        [SerializeField] private Slider pvSlider;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private PlayerData playerData;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                pvSlider.gameObject.SetActive(false);
                return;
            }

            pvSlider.gameObject.SetActive(true);

            ListenToEvent<PlayerDataEvent>(UpdateUI);
            
            if (playerData == null)
                playerData = GetComponent<PlayerData>();

            if (playerData != null)
            {
                UpdateUI(new PlayerDataEvent
                {
                    playerHp = playerData.CurrentHp,
                    maxHp = playerData.MaxHp
                });
            }
        }

        private void UpdateUI(PlayerDataEvent e)
        {
            if (pvSlider == null || playerHpText == null) return;

            pvSlider.maxValue = e.maxHp;
            pvSlider.value = e.playerHp;
            playerHpText.text = e.playerHp.ToString();
        }
    }
}