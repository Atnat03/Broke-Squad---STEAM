    using System;
    using Bus;
    using Gameplay.LD.Scripts;
    using Unity.Netcode;
    using UnityEngine;
    using Random = UnityEngine.Random;

    namespace Gameplay.PlayerData
    {
        public class PlayerData : NetworkBusListener, IDamageable
        {
            [SerializeField] private const int MaxHpValue = 100;
            
            [Header("SFX")]
            [SerializeField, SoundName] private string[] _takeDamageSound;
            private int _previousTakeDamageSoundPlayed = -1;
            [SerializeField, SoundName] private string _deathSound;
            [SerializeField, SoundName] private string _rezSound;

            private readonly NetworkVariable<int> _playerHp =
                new NetworkVariable<int>(
                    MaxHpValue,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server);

            private readonly NetworkVariable<bool> _playerDead =
                new NetworkVariable<bool>(
                    false,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server);

            public NetworkVariable<bool> invincibility =
                new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server);
            
            public int CurrentHp => _playerHp.Value;
            public int MaxHp => MaxHpValue;

            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                ListenToEvent<PlayerRevivedEvent>(Revive);
                _playerHp.OnValueChanged += OnHpChanged;
                invincibility.OnValueChanged += OnInvincibilityChanged;
            }

            public override void OnNetworkDespawn()
            {
                _playerHp.OnValueChanged -= OnHpChanged;
                
                base.OnNetworkDespawn();
            }

            [ContextMenu("Test")]
            public void Test()
            {
                ApplyDamage(100);
            }
            
            public void ApplyDamage(float damage)
            {
                if (!IsServer || damage <= 0 || _playerDead.Value || invincibility.Value)
                    return;

                _playerHp.Value = Mathf.Max(0, _playerHp.Value - (int)damage);

                ReplicateTakingDamageRpc();
                
                if (_playerHp.Value <= 0)
                    SetPlayerDead(true);
            }

            [Rpc(SendTo.Everyone)]
            private void ReplicateTakingDamageRpc()
            {
                int newIdForTakeDamage;

                do
                {
                    newIdForTakeDamage = Random.Range(0, _takeDamageSound.Length);
                } 
                while (newIdForTakeDamage == _previousTakeDamageSoundPlayed);
                
                InvokeEvent(new PlaySoundEvent
                {
                    soundName = _takeDamageSound[newIdForTakeDamage],
                    position = transform.position,
                    volume = 0.3f
                });
                
                _previousTakeDamageSoundPlayed = newIdForTakeDamage;
            }

            public void Heal(float amount)
            {
                if (!IsServer || amount <= 0 || _playerDead.Value)
                    return;

                _playerHp.Value = Mathf.Min(_playerHp.Value + (int)amount, MaxHpValue);
            }
            

            private void SetPlayerDead(bool dead)
            {
                if (_playerDead.Value == dead)
                    return;

                _playerDead.Value = dead;

                if (dead)
                {
                    NotifyDeathOwnerRpc();
                    ReplicateDeathRpc();
                }
            }
            
            private void OnHpChanged(int previousHp, int newHp)
            {
                if (!IsOwner)
                    return;

                PublishHp();
            }

            private void OnInvincibilityChanged(bool previousBool, bool newBool)
            {
                if (!IsOwner)
                    return;
                
                PublishHp();
            }

            private void PublishHp()
            {
                InvokeEvent(new PlayerDataEvent
                {
                    playerHp = _playerHp.Value,
                    maxHp = MaxHpValue
                });
                
                InvokeEvent(new PlayerStatusChangedEvent()
                {
                    invincible = invincibility.Value,
                    playerID = OwnerClientId
                });
            }

            private void Revive(PlayerRevivedEvent e)
            {
                if (_playerDead.Value == true)
                {
                    _playerHp.Value = MaxHp;
                    _playerDead.Value = false;
                }
               
            }
            
            [Rpc(SendTo.Owner)]
            private void NotifyDeathOwnerRpc()
            {
                InvokeEvent(new PlayerDeathEvent
                {
                    playerID = OwnerClientId
                });
            }

            [Rpc(SendTo.Everyone)]
            private void ReplicateDeathRpc()
            {
                InvokeEvent(new PlaySoundEvent
                {
                    soundName = _deathSound,
                    position = transform.position,
                    volume = 0.5f
                });
            }
            
            public bool IsDead() => _playerDead.Value;
        }
    }