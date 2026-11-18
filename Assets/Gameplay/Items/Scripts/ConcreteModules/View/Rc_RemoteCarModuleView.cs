using Bus;
using UnityEngine;

namespace Gameplay.Items
{
    public class Rc_RemoteCarModuleView : MonoBusListener
    {
        [SerializeField] private string _keyEvent = "RC_CAR_ANIM_EVENT";
        [SerializeField] private Animator _animator;

        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>((data =>
            {
                _animator?.SetBool(data.ValueS.Value, data.ValueB);
            }));
        }
    }
}