using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Gameplay.LD
{
    public abstract class LDElementModule : MonoBehaviour
    {
        protected LDElementContext Context { get; private set; }
        public int ModuleID { get; private set; }
        public void Initialize(LDElementContext context, ModuleConfig config)
        {
            Context = context;
            ModuleID = config.ModuleID;
            OnInitialize(config);
        }

        protected abstract void OnInitialize(ModuleConfig config);
    }
}
