using Bus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

struct SensitivityChange_EVENT
{
};

namespace Gameplay.UI.Scripts
{
    public class SensitivySlider : MonoBusListener
    {
        public const string Key = "MouseSensitivity";

        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueLabel; // optional

        private void Start()
        {
            slider.minValue = 0.1f;
            slider.maxValue = 3f;
            float saved = PlayerPrefs.GetFloat(Key, 1f);
            slider.SetValueWithoutNotify(saved);
            UpdateLabel(saved);
            slider.onValueChanged.AddListener(OnChanged);
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(OnChanged);
            PlayerPrefs.Save();
        }

        private void OnChanged(float v)
        {
            PlayerPrefs.SetFloat(Key, v);
            UpdateLabel(v);
            InvokeEvent(new SensitivityChange_EVENT());
        }

        private void UpdateLabel(float v)
        {
            if (valueLabel) valueLabel.text = v.ToString("0.00") + "x";
        }
    }
}