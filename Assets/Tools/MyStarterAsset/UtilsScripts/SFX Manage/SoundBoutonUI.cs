using Bus;
using UnityEngine;
using UnityEngine.UI;

namespace SFX_Manage 
{
    [RequireComponent(typeof(Button))]
    public class SoundBoutonUI : MonoBehaviour
    {
        [SerializeField] private string nameSound;

        private Button button;

        private void Start()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(PlaySound);
            }
        }

        private void PlaySound()
        {
            EventBus.InvokeEvent(new PlaySoundEvent { soundName = nameSound, position = transform.position });
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlaySound);
            }
        }
    }
}
