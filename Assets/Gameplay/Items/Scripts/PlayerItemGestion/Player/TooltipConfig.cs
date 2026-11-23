using System;
using Gameplay.LD;
using MyPrint;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public struct Tooltip
    {
        public string Message;
        public Sprite Icon;
    }
    
    [Serializable]
    public class TooltipConfig
    {
        [Header("Default Tooltip")] 
        [SerializeField] public Tooltip DefaultTooltip;
        
        [Header("Is Item")] 
        [SerializeField] public bool IsItem;
        [SerializeField] public Tooltip TooltipInHand;

        [Header("Item Requirement")] 
        [SerializeField] private bool _requiresItemModule;
        [SerializeField] private Tooltip _tooltipWithItem;
        [SerializeReference][SubclassSelector] private ItemModule _requiredModule;
        
        public Tooltip GetTooltip(ItemModule[] playerModules)
        {
            if (_requiresItemModule)
            {
                if (_requiredModule == null)
                    return DefaultTooltip;

                if (playerModules == null)
                    return DefaultTooltip;

                foreach (ItemModule module in playerModules)
                {
                    if (module != null && module.GetType() == _requiredModule.GetType())
                    {
                        return _tooltipWithItem;
                    }
                }
            }

            return DefaultTooltip;
        }
    }
}