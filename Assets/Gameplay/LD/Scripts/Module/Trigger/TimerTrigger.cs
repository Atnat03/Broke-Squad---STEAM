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
        float _interval;

        protected override void OnInitialize(ModuleConfig config)
        {
            _interval = (config as TimerTriggerConfig).Interval;
            if(Context.IsServer) StartCoroutine(TimerCoroutine());
        }

        private IEnumerator TimerCoroutine()
        {
            while(true)
            {
                yield return new WaitForSeconds(_interval);
                Debug.Log("[LD] tick");
                Raise(new LDActivation { Position = transform.position });
            }
        }
    }
}
