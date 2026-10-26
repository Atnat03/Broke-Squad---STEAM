using UnityEngine;

namespace Gameplay.LD
{
    public class FlagCondition : LDConditionModule
    {
        [SerializeField] private LDFlag _flag = LDFlag.Armed;
        [SerializeField] private bool _required = true;

        public override bool CanActivate(LDActivation activation) => Context.HasFlag(_flag) == _required;
    }
}