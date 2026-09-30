using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public class BreakableWindow : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _health;
        
        public void ApplyDamage(float damage)
        {
            _health -= damage;

            if (_health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}