using System;
using Bus;
using MyPrint;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LimitFpsSettings : MonoBusListener
{
    [Header("Settings")]
    [SerializeField] private int _minimumFpsAvailable = 15;
    [SerializeField] private int _maximumFpsAvailable = 120;

    private int _currentFps = 60;

    [Header("UI")]
    [SerializeField] private Slider _sliderFps;
    [SerializeField] private TMP_InputField _inputTextFps;

    private void Start()
    {
        _sliderFps.minValue = 0f;
        _sliderFps.maxValue = 1f;

        UpdateFps();
    }

    public void ManuallySetFps()
    {
        if (!int.TryParse(_inputTextFps.text, out int fps))
            return;

        _currentFps = Mathf.Clamp(fps, _minimumFpsAvailable, _maximumFpsAvailable);

        UpdateFps();
    }

    public void SetFpsBySlider()
    {
        _currentFps = Mathf.RoundToInt(Mathf.Lerp(_minimumFpsAvailable, _maximumFpsAvailable, _sliderFps.value));

        UpdateFps();
    }

    private void UpdateFps()
    {
        _currentFps = Mathf.Clamp(_currentFps, _minimumFpsAvailable, _maximumFpsAvailable);

        Application.targetFrameRate = _currentFps;

        _inputTextFps.SetTextWithoutNotify(_currentFps.ToString());
        float ratio = Mathf.InverseLerp(_minimumFpsAvailable, _maximumFpsAvailable, _currentFps);

        _sliderFps.SetValueWithoutNotify(ratio);
    }

    public void ToggleFpsCounter(Toggle toggle)
    {
        InvokeEvent(new FPSCounterChangeVisual_EVENT
        {
            show = toggle.isOn
        });
    }
}