using System;
using Bus;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace SFX_Manage
{
    public class AudioSettingsMenu : MonoBehaviour
    {
        [Serializable]
        private class ChannelUI
        {
            public string exposedParam;
            public Slider slider;
            public Toggle muteToggle;
            public TextMeshProUGUI volumeText;
            
            [NonSerialized] public float volume;
            [NonSerialized] public bool muted;
        }
        
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private ChannelUI[] channels;
        

        private void Start()
        {
            foreach (ChannelUI ui in channels)
            {
                ChannelUI c = ui; 

                c.volume = PlayerPrefs.GetFloat(c.exposedParam + "_vol", 1f);
                c.muted = PlayerPrefs.GetInt(c.exposedParam + "_mute", 0) == 1;

                if (c.slider != null)
                {
                    c.slider.minValue = 0f;
                    c.slider.maxValue = 1f;
                    c.slider.SetValueWithoutNotify(c.volume);
                    c.slider.onValueChanged.AddListener(v => { c.volume = v; Apply(c); });
                }

                if (c.muteToggle != null)
                {
                    c.muteToggle.SetIsOnWithoutNotify(c.muted);
                    c.muteToggle.onValueChanged.AddListener(m => { c.muted = m; Apply(c); });
                }

                Apply(c);
            }
        }

        private void Apply(ChannelUI c)
        {
            float dB = c.muted ? -80f : Mathf.Log10(Mathf.Clamp(c.volume, 0.0001f, 1f)) * 20f;
            mixer.SetFloat(c.exposedParam, dB);

            if (c.volumeText != null)
                c.volumeText.text = $"{Mathf.RoundToInt(c.volume * 100)}%";

            PlayerPrefs.SetFloat(c.exposedParam + "_vol", c.volume);
            PlayerPrefs.SetInt(c.exposedParam + "_mute", c.muted ? 1 : 0);
        }

        private void OnDisable() => PlayerPrefs.Save();
    }
}