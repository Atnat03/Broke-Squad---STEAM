using System;
using Gameplay.Items.Scripts.PlayerItemGestion;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class BreakThings : ItemModule, ILeftClick
    {
        [SerializeField] private float _strength = 10;
        
        public void StartLeftClick()
        {
            RaycastHit hit;
            
            if (Physics.Raycast(Context.Camera.transform.position, Context.Camera.transform.forward,out hit, 2))
            {
                ABPrint.Print("Hit " + hit.transform.name, ABColor.Green);
                
                if (hit.transform.TryGetComponent(out Breakable wall))
                {
                    wall.Break(hit.point, _strength);
                }
            }
        }

        public void EndLeftClick()
        { }
    }
}