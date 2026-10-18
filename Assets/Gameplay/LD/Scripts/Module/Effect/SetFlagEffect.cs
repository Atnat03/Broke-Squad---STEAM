using UnityEngine;

namespace Gameplay.LD
{
    public class SetFlagEffect : LDEffectModule
    {
        [SerializeField] private LDFlag _flag = LDFlag.Armed;
        [SerializeField] private bool _value;
        [SerializeField] private bool _toggle;

        public override void Activate(LDActivation activation)
        {
            bool value = _toggle ? !Context.HasFlag(_flag) : _value;
            Context.SetFlag(_flag, value);
        }
    }
}