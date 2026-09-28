using UnityEngine;

namespace Gameplay.Items
{
    public abstract class ItemModule : MonoBehaviour
    {
        protected ItemCore _core;
        
        [SerializeField] private string moduleName = "";
        [SerializeField] private Color moduleColor = Color.clear;
        
        public string ModuleName => moduleName;
        public Color ModuleColor => moduleColor;
        
        public void Initialize(ItemCore core)
        {
            _core = core;
        }
    }
}