namespace Gameplay.LD
{
    public class SetFlagEffect : LDEffectModule
    {
        private LDFlag _flag;
        private bool _value;
        private bool _toggle;

        protected override void OnInitialize(ModuleConfig config)
        {
            SetFlagEffectConfig c = (SetFlagEffectConfig)config;
            _flag = c.Flag;
            _value = c.Value;
            _toggle = c.Toggle;
        }

        public override void Activate(LDActivation activation)
        {
            bool value = _toggle ? !Context.HasFlag(_flag) : _value;
            Context.SetFlag(_flag, value);
        }
    }
}