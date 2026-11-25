using Bus;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class TriggerSoundView : MonoBusListener
    {
        private string _key = "PLAY_SOUND_EVENT";
        ItemInstance _itemInstance;
        
        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(TriggerSound);
        }

        private void TriggerSound(OnModuleDoAction_EVENT data)
        {
            if(_itemInstance == null)
                _itemInstance = GetComponentInParent<ItemCore>().Instance;
            
            if (data.KeyEvent != _key) return;
            if (_itemInstance.InstanceId != data.InstanceId) return;
            if (_itemInstance == null) return;
            
            InvokeEvent(new PlaySoundEvent
            {
                Position = data.Position,
                Volume = data.ValueI,
                SoundName = data.ValueS.Value
            });
        }
    }
}