using Bus;
using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public class Door : MonoBusListener
    {
        [SerializeField] private int _doorID;
        [SerializeField] private Animator _animator;
        
        public bool TryOpen(int id)
        {
            if (id == _doorID)
            {
                _animator.SetTrigger("Open");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}