using Gameplay.LD.Scripts;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class OpenDoorModule : ItemModule, ILeftClick
    {
        [SerializeField] private int _doorId;
        
        public void StartLeftClick()
        {
            RaycastHit hit;
            
            if (Physics.Raycast(Context.Camera.transform.position, Context.Camera.transform.forward,out hit, 2))
            {
                ABPrint.Print("Hit " + hit.transform.name, ABColor.Green);
                
                if (hit.transform.TryGetComponent(out Door door))
                {
                    if (door.TryOpen(_doorId))
                    {
                        Context.Inventory.DestroyItemInHand();
                    }
                }
            }
        }

        public void EndLeftClick()
        { }
    }
}