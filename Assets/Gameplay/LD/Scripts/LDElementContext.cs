using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public class LDElementContext
    {
        public LDElement Element { get;}

        public LDElementContext(LDElement element)
        {
            Element = element;
        }

        public bool IsServer => Element.IsServer;

        public bool HasFlag(LDFlag flag) => Element.HasFlag(flag);

        public void SetFlag(LDFlag flag, bool value) => Element.SetFlag(flag, value);

        internal void RequestInteract(LDElementModule module) => Element.RequestInteract(module);

        public event Action<LDFlag> OnFlagChanged
        {
            add => Element.FlagsChanged += value;
            remove => Element.FlagsChanged -= value;
        }
    }
}
