using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class LogMsgEffect : LDEffectModule
    {
        private string _message;
        public override void Activate(LDActivation activation)
        {
            Debug.Log(_message);
        }

        protected override void OnInitialize(ModuleConfig config)
        {
            var c = (LogMsgEffectConfig)config;
            _message = c.Message;
        }
    }
}
