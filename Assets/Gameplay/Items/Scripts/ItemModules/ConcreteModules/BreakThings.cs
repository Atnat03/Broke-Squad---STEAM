using MyPrint;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class BreakThings : ItemModule, ILeftClick
    {
        public void StartLeftClick()
        {
            ABPrint.Print("Start Left Click", ABColor.Green);
        }

        public void EndLeftClick()
        {
            ABPrint.Print("End Left Click", ABColor.Green);
        }
    }
}