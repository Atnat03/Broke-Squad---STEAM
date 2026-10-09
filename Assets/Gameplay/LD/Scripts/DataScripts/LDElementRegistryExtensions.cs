using System;
using System.Collections.Generic;
using System.Text;

namespace Gameplay.LD
{
    public static class LDElementRegistryExtensions
    {
        public static int IndexOf(this LDElementRegistry registry, SO_LDElementData data) => registry.LDElementsList.IndexOf(data);

        public static SO_LDElementData Get(this LDElementRegistry registry, int index) => 
            index >= 0 && index < registry.LDElementsList.Count ? registry.LDElementsList[index] : null;
    }
}
