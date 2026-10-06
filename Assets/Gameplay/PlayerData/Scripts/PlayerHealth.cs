    using System;
    using Bus;
    using Gameplay.LD.Scripts;
    using Unity.Netcode;
    using UnityEngine;
    using Random = UnityEngine.Random;

    namespace Gameplay.PlayerData
    {
        public class PlayerHealth : NetworkBusListener, IDamageable
        {
            [SerializeField] private const int MaxHpValue = 100;
            
            private int _previousTakeDamageSoundPlayed = -1;
            [Header("SFX")]
            [SerializeField, SoundName] private string[] _takeDamageSound;
            [SerializeField, SoundName] private string _deathSound;
            [SerializeField, SoundName] private string _rezSound;

            private readonly NetworkVariable<int> _playerHp = new NetworkVariable<int>(MaxHpValue,
                    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            private readonly NetworkVariable<bool> _playerDown = new NetworkVariable<bool>(false,
                    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            private readonly NetworkVariable<bool> _playerInvincible = new NetworkVariable<bool>(false, 
                     NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
            public int CurrentHp => _playerHp.Value;
            public int MaxHp => MaxHpValue;
            public bool IsDowned => _playerDown.Value;
            public bool IsInvincible => _playerInvincible.Value;

            public event Action<int, int> OnChangeHp;
            public event Action<bool> OnChangedInvincibility;
            public event Action<bool> OnChangedDown;

            #region Subs
            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                if (IsServer) _playerHp.Value = MaxHpValue;

                _playerHp.OnValueChanged += OnHpChanged;
                _playerInvincible.OnValueChanged += OnInvincibleChanged;
                _playerDown.OnValueChanged += OnDownedChanged;
            }

            public override void OnNetworkDespawn()
            {
                _playerHp.OnValueChanged -= OnHpChanged;
                _playerInvincible.OnValueChanged -= OnInvincibleChanged;
                _playerDown.OnValueChanged -= OnDownedChanged;
                
                base.OnNetworkDespawn();
            }

            #endregion

            #region Servers Functions

            public void ApplyDamage(float damage)
            {
                if (!IsServer || damage <= 0 || _playerDown.Value || _playerInvincible.Value) return;

                _playerHp.Value = Mathf.Max(0, _playerHp.Value - (int)damage);
                PlayHurtRpc();
                
                if (_playerHp.Value <= 0)
                {
                    _playerDown.Value = true;
                    PlayDeathRpc();
                }
            }
            
            public void Heal(float amount)
            {
                if (!IsServer || amount <= 0f || _playerDown.Value) return;
                _playerHp.Value = Mathf.Min(_playerHp.Value + Mathf.CeilToInt(amount), MaxHpValue);
            }
            
            public void SetInvincible(bool value)
            {
                if (!IsServer) return;
                _playerInvincible.Value = value;
            }

            public void ServerRevive()
            {
                if (!IsServer || !_playerDown.Value) return;

                _playerHp.Value = MaxHpValue;
                _playerDown.Value = false;
                
                InvokeEvent(new PlayerRevivedEvent { playerID = OwnerClientId });
            }
            #endregion
            
            #region Replication callbacks (every client)

            private void OnHpChanged(int previous, int current) => OnChangeHp?.Invoke(previous, current);

            private void OnInvincibleChanged(bool previous, bool current) => OnChangedInvincibility?.Invoke(current);

            private void OnDownedChanged(bool previous, bool downed)
            {
                OnChangedDown?.Invoke(downed);
                
                if (downed && IsOwner)
                    InvokeEvent(new PlayerDeathEvent { playerID = OwnerClientId });
            }
            
            #endregion
           
            #region SFX
            [Rpc(SendTo.Everyone)]
            private void PlayDeathRpc() => PlaySfx(_deathSound, 0.5f);
            
            [Rpc(SendTo.Owner)]
            private void PlayHurtRpc()
            {
                if (_takeDamageSound == null || _takeDamageSound.Length == 0) return;

                int id = Random.Range(0, _takeDamageSound.Length);
                if (_takeDamageSound.Length > 1 && id == _previousTakeDamageSoundPlayed)
                    id = (id + Random.Range(1, _takeDamageSound.Length)) % _takeDamageSound.Length;
                _previousTakeDamageSoundPlayed = id;

                PlaySfx(_takeDamageSound[id], 0.3f);
            }
            
            private void PlaySfx(string sound, float volume)
            {
                if (string.IsNullOrEmpty(sound)) return;

                InvokeEvent(new PlaySoundEvent
                {
                    soundName = sound,
                    position = transform.position,
                    volume = volume
                });
            }
            
            #endregion
            
        }
    }