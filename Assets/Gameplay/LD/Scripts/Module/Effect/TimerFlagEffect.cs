using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.LD
{
    public class TimerFlagEffect : LDEffectModule
    {
        [SerializeField] private LDFlag _flag = LDFlag.Activated;

        [SerializeField, Min(0f)] private float _timerDuration = 5f;

        [Tooltip("Coché = le timer se reset si press")]
        [SerializeField] private bool _restartOnPress;

        private Coroutine _couroutine;

        public override void Activate(LDActivation activation)
        {
            if (Context.HasFlag(_flag) && !_restartOnPress) return;

            Context.SetFlag(_flag, true);
            if (_couroutine != null) StopCoroutine(_couroutine);
            _couroutine = StartCoroutine(ActiveDuration());
        }

        private IEnumerator ActiveDuration()
        {
            yield return new WaitForSeconds(_timerDuration);

            Context.SetFlag(_flag, false);
            _couroutine = null;
        }
    }
}
