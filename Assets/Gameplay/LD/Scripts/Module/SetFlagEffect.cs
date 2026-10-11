namespace Gameplay.LD
{
    public class SetFlagEffect : LDEffectModule
    {
        private LDFlag _flag;
        private bool _value;

        protected override void OnInitialize(ModuleConfig config)
        {
            SetFlagEffectConfig c = (SetFlagEffectConfig)config;
            _flag = c.Flag;
            _value = c.Value;
        }

        public override void Activate(LDActivation activation) => Context.SetFlag(_flag, _value);
    }
}