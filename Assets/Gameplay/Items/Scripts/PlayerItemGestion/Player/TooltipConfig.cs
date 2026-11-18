using System;
using Gameplay.LD;
using MyPrint;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class TooltipConfig
    {
        [Header("Default Tooltip")] 
        [TextArea] [SerializeField] public string DefaultText;

        [Header("Is Item")] 
        [SerializeField] public bool IsItem;
        [SerializeField] public string TooltipInHand;

        [Header("Item Requirement")] 
        [SerializeField] private bool _requiresItemModule;

        [TextArea] [SerializeField] private string _tooltipWithItem;
        [SerializeReference][SubclassSelector] private ItemModule _requiredModule;
        
        public string GetTooltip(ItemModule[] playerModules)
        {
            if (_requiresItemModule)
            {
                if (_requiredModule == null)
                    return DefaultText;

                if (playerModules == null)
                    return DefaultText;

                foreach (ItemModule module in playerModules)
                {
                    if (module != null && module.GetType() == _requiredModule.GetType())
                    {
                        return _tooltipWithItem;
                    }
                }
            }

            return DefaultText;
        }
    }
}