using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace Gameplay.LD.Scripts
{
    public class Outliner : MonoBehaviour
    {
        [SerializeField] private Outline _outline;
        
        void Awake()
        {
            SetOutline(false);
        }
        
        public void SetOutline(bool state)
        {
            if(_outline !=  null)
                _outline.enabled = state;
        }
    }
}