using Bus;
using Gameplay.Items;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

public class ThrowModuleView : MonoBusListener
{
    [SerializeField] private string _eventKey = "UI_THROW_BAR";
    
    [Header("References")]
    [SerializeField] private GameObject _uiThrow;
    [SerializeField] private Image _throwImage;

    void OnEnable()
    {
        ListenToEvent<OnModuleDoAction_EVENT>(UpdateBar);
    }

    private void UpdateBar(OnModuleDoAction_EVENT data)
    {
        _uiThrow.gameObject.SetActive(data.ValueB);
        _throwImage.fillAmount = data.ValueF;
    }
}
