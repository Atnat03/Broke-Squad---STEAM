using UnityEngine;

namespace Gameplay.Controller.Minimap
{
    public class FreezeRotationMinimap : MonoBehaviour
    {
        [SerializeField] Transform _target;
        
        public void SetTarget(Transform target) => _target = target;
        
        void LateUpdate()
        {
            if (!_target) return;
            
            Vector3 pos = _target.position;
            pos.y =  transform.position.y;
            transform.position = pos;
        }
    }
}