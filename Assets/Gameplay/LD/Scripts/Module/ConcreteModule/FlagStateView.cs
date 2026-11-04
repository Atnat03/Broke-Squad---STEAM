using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD
{
    public class FlagStateView : LDElementModule
    {
        [SerializeField] private LDFlag _flag = LDFlag.Active;
        [SerializeField] private UnityEvent _onSet;
        [SerializeField] private UnityEvent _onCleared;

        private bool _state;

        protected override void OnInitialize()
        {
            _state = Context.HasFlag(_flag);
            Apply(_state);
            Context.OnFlagChanged += OnFlagsChanged;
        }

        private void OnFlagsChanged(LDFlag flags)
        {
            bool state = (flags & _flag) != 0;
            if (state == _state) return;
            _state = state;
            Apply(state);
        }

        private void Apply(bool state)
        {
            if (state) _onSet.Invoke();
            else _onCleared.Invoke();
        }
    }
}