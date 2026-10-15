using MyPrint;
using Unity.Netcode;

namespace Gameplay.LD
{
    public class TriggerEventEffect : LDEffectModule
    {
        private bool _isOpen;
        private string key;

        protected override void OnInitialize(ModuleConfig config)
        {
            key = ((TriggerEventEffectConfig)config).KeyEvent;
        }
        
        public override void Activate(LDActivation activation)
        {
            if (Context.IsServer)
            {
                _isOpen = !_isOpen;
                
                InvokeEvent(new On_LD_TriggerEventEffect_EVENT
                {
                    Key = key,
                    state = _isOpen
                });        
            }
        }
    }
}