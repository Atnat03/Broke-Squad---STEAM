using UnityEngine;

namespace Gameplay.LD
{
    public class SetFlagEffect : LDEffectModule
    {
        [Tooltip("Flag a modifié")]
        [SerializeField] private LDFlag _flag = LDFlag.Armed;
        [Tooltip("Valeur appliquée au flag, ignoré si toogle est coché")]
        [SerializeField] private bool _value;
        [Tooltip("Inverse le flag à chaque activation")]
        [SerializeField] private bool _toggle;

        public override void Activate(LDActivation activation)
        {
            bool value = _toggle ? !Context.HasFlag(_flag) : _value;
            Context.SetFlag(_flag, value);
        }
    }
}