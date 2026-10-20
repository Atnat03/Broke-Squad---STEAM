using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class TriggerEventEffect : LDEffectModule
    {
        [SerializeField] private string key = "LD_TRIGGER_EVENT";
        private bool _isOpen;

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