using Bus;
using UnityEngine;

namespace Gameplay.Items
{
    public class Rc_RemoteCarModuleView : MonoBusListener
    {
        [SerializeField] private string _keyEvent = "RC_CAR_ANIM_EVENT";
        
        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>((data =>
            {
                if(data.KeyEvent != _keyEvent) return;
                if (data.InstanceId != transform.root.GetComponentInChildren<ItemCore>().Instance.InstanceId) return;
                
                GetComponent<Animator>()?.SetBool(data.ValueS.Value, data.ValueB);
            }));
        }
    }
}