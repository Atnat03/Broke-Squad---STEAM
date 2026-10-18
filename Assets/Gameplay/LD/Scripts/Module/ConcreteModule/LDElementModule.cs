using System;
using System.Collections.Generic;
using System.Text;
using Bus;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public abstract class LDElementModule : NetworkBusListener
    {
        protected LDElementContext Context { get; private set; }
        public void Initialize(LDElementContext context)
        {
            Context = context;
            OnInitialize(config);
        }

        protected abstract void OnInitialize();
    }
}
