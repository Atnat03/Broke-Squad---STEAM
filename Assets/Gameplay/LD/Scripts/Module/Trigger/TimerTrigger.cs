using Gameplay.LD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class TimerTrigger : LDTriggerModule
    {
        [SerializeField] private float _interval = 2f;

        protected override void OnInitialize()
        {
            if (Context.IsServer) StartCoroutine(TimerCoroutine());
        }

        private IEnumerator TimerCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_interval);
                Raise(new LDActivation(null, transform.position));
            }
        }
    }
}
