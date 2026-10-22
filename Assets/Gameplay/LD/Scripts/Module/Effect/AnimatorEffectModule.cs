using UnityEngine;

namespace Gameplay.LD
{
    public class AnimatorEffectModule : LDEffectModule
    {
        [SerializeField] private Animator _animator;

        [Tooltip("Le flag suivi par l'animation.")]
        [SerializeField] private LDFlag _flag = LDFlag.Active;

        [Tooltip("Trigger lancé quand le flag est actif (ex. Open).")]
        [SerializeField] private string _triggerOpen = "Open";

        [Tooltip("Trigger lancé quand le flag est retiré (ex. Close).")]
        [SerializeField] private string _triggerClose = "Close";

        private int _setHash;
        private int _clearedHash;
        private bool _state;

        protected override void OnInitialize()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"[LD] {name} : Animator manquant", this);
                return;
            }

            _setHash = Animator.StringToHash(_triggerOpen);
            _clearedHash = Animator.StringToHash(_triggerClose);

            _state = Context.HasFlag(_flag);
            Apply(_state);
            Context.OnFlagChanged += OnFlagsChanged;
        }

        private void OnFlagsChanged(LDFlag flags)
        {
            Debug.Log("[LD] Animation Active");

            bool state = (flags & _flag) != 0;
            if (state == _state) return;
            _state = state;
            Apply(state);
        }

        private void Apply(bool state)
        {
            _animator.ResetTrigger(state ? _clearedHash : _setHash);
            _animator.SetTrigger(state ? _setHash : _clearedHash);
        }

        public override void Activate(LDActivation activation)
        {
            
        }
    }
}