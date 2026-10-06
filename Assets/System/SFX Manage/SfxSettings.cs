using Bus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFX_Manage
{
    public class SfxSettings : MonoBehaviour
    {
        [Header("SFX Controls")]
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle sfxMuteToggle;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;

        private const string VolumeKey = "sfx_volume";
        private const string MuteKey = "sfx_muted";

        private float volume = 1f;
        private bool isMuted;

        private void Start()
        {
            volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            isMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.SetValueWithoutNotify(volume);
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(isMuted);
                sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);
            }

            RefreshText();
            Publish(); 
        }

        private void OnSFXVolumeChanged(float value) { volume = value; RefreshText(); Publish(); }
        private void OnSFXMuteChanged(bool isMuted) { this.isMuted = isMuted; Publish(); }

        private void RefreshText()
        {
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }

        private void Publish()
        {
            EventBus.InvokeEvent(new SfxSettingsChangedEvent { volume = volume, muted = isMuted });
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        }
    }
}