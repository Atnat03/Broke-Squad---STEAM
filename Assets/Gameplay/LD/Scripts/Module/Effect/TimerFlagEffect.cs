using System;
using System.Collections;
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

            Debug.Log("[LD] Activation Timer lunch " + "flag " + Context.HasFlag(_flag));
        }

        private IEnumerator ActiveDuration()
        {
            yield return new WaitForSeconds(_timerDuration);

            Context.SetFlag(_flag, false);
            Debug.Log("[LD] Activation Timer finish " + "flag : " + Context.HasFlag(_flag));
            _couroutine = null;
        }
    }
}
