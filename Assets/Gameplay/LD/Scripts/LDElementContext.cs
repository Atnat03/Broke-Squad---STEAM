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
    }
}
