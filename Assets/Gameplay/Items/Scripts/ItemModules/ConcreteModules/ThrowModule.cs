using System;
using MyPrint;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class ThrowModule : ItemModule, IRightClick
    {
        public void StartRightClick()
        {
            ABPrint.Print("Start Right Click", ABColor.Red);
        }

        public void EndRightClick()
        {
            ABPrint.Print("End Right Click", ABColor.Red);
        }
    }
}