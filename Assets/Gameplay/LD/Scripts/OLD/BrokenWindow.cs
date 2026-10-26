using Bus;
using Gameplay.LD.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Gameplay.LD.Scripts
{
    [RequireComponent(typeof(NetworkObject))]
    public class BrokenWindow : NetworkBusListener, IDamageable
    {
        [SerializeField] private float solidity = 100.0f;
        [SerializeField] private UnityEvent _brokenEvent;
        [SerializeField, SoundName] private string _breakSound;

        private readonly NetworkVariable<bool> isBroken = new(false);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            isBroken.OnValueChanged += OnBrokenChanged;
        }

        public override void OnNetworkDespawn()
        {
            isBroken.OnValueChanged -= OnBrokenChanged;
            base.OnNetworkDespawn();
        }

        public void ApplyDamage(float damage)
        {
            if (!IsServer || isBroken.Value) {return;}
            
            if (damage >= solidity) Break();
        }

        private void Break()
        {
            // Proto 
            isBroken.Value = true;
            
            NetworkObject.Despawn();

            // Plus tard 
        }

        private void OnBrokenChanged(bool previous, bool current)
        {
            if (!current) return;
            
            _brokenEvent?.Invoke();
            
            InvokeEvent(new PlaySoundEvent
            {
                soundName = _breakSound,
                position = transform.position,
                volume = 0.5f
            });

            //brokenWAnimator.Play(brokenStateName, 0);
            //GetComponent<Collider>().enabled = false;
        }
    }
}
