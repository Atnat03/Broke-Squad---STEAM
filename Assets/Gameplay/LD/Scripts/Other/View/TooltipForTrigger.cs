using Bus;
using UnityEngine;

namespace Gameplay.LD
{
    public class TooltipForTrigger : MonoBusListener
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject _hasntTheGoodItemPannel;

        public void SetUp(Sprite icon, bool hasTheItem)
        {
            spriteRenderer.sprite = icon;
            _hasntTheGoodItemPannel.SetActive(!hasTheItem);
        }

        public void StartDestroy()
        {
            if(TryGetComponent(out Animator a))
                a.SetTrigger("Disable");
        }
        
        public void DestroyIt() => Destroy(gameObject);
    }
}