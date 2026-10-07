using System;
using Bus;
using Gameplay.Items;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

public class ThrowModuleView : MonoBusListener
{
    private string _eventKey = "UI_THROW_BAR";
    
    [Header("References")]
    [SerializeField] private GameObject _uiThrow;
    [SerializeField] private Image _throwImage;

    private void Awake()
    {
        _uiThrow.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        ListenToEvent<OnModuleDoAction_EVENT>(UpdateBar);
    }
    
    private void UpdateBar(OnModuleDoAction_EVENT data)
    {
        if(_eventKey != data.KeyEvent)
            return;
        
        _uiThrow.gameObject.SetActive(data.ValueB);
        _throwImage.fillAmount = data.ValueF;
    }
}
