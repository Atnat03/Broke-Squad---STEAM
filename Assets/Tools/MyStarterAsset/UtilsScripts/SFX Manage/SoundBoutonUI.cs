using Bus;
using UnityEngine;
using UnityEngine.UI;

namespace SFX_Manage 
{
    [RequireComponent(typeof(Button))]
    public class SoundBoutonUI : MonoBehaviour
    {
        [SerializeField, SoundName] private string nameSound;

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
            EventBus.InvokeEvent(new PlaySoundEvent { soundName = nameSound, position = transform.position, is2D = true });
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
