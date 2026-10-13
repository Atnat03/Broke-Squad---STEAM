namespace Gameplay.LD
{
    public class FlagCondition : LDConditionModule
    {
        private LDFlag _flag;
        private bool _required;

        protected override void OnInitialize(ModuleConfig config)
        {
            var condition = (FlagConditionConfig)config;
            _flag = condition.Flag;
            _required = condition.Required;
        }

        public override bool CanActivate(LDActivation activation)
            => Context.HasFlag(_flag) == _required;
    }
}