    using System;
    using System.Collections;
    using Bus;
    using Gameplay.Controller;
    using Gameplay.LD.Scripts;
    using MyPrint;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Random = UnityEngine.Random;

    namespace Gameplay.PlayerData
    {
        public class PlayerHealth : NetworkBusListener, IDamageable
        {
            [SerializeField] private float MaxHpValue = 100;
            private int _previousTakeDamageSoundPlayed = -1;
            
            [Header("References")]
            [SerializeField] private PlayerRevive _playerRevive;
            
            [Header("SFX")]
            [SerializeField, SoundName] private string[] _takeDamageSound;
            [SerializeField, SoundName] private string _deathSound;
            [SerializeField, SoundName] private string _rezSound;
            [SerializeField] private MeshRenderer _bodyMesh;

            private readonly NetworkVariable<float> _playerHp = new ();

            private readonly NetworkVariable<bool> _playerDown = new NetworkVariable<bool>(false,
                    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

            private readonly NetworkVariable<bool> _playerInvincible = new NetworkVariable<bool>(false, 
                     NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
            public float CurrentHp => _playerHp.Value;
            public float MaxHp => MaxHpValue;
            public bool IsDowned => _playerDown.Value;
            public bool IsInvincible => _playerInvincible.Value;
            public PlayerRevive PlayerRevive => _playerRevive;

            private bool _canTakeDamage = true;

            public event Action<float, float> OnChangeHp;
            public event Action<bool> OnChangedInvincibility;
            public event Action<bool> OnChangedDown;

            #region Subs
            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                _playerHp.OnValueChanged += OnHpChanged;
                
                if (IsServer)
                    _playerHp.Value = MaxHpValue;

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

            public void ApplyDamage(float damage, NetworkObject source = null)
            {
                if (!IsServer || damage <= 0 || _playerDown.Value || _playerInvincible.Value) return;
                if (!_canTakeDamage) return;

                _playerHp.Value = Mathf.Max(0, _playerHp.Value - (int)damage);
                PlayHurtRpc();
                HitMarkClientRpc();

                StartCoroutine(BuggerTakeDamage());
                
                if (_playerHp.Value <= 0)
                {
                    _playerDown.Value = true;
                    PlayDeathRpc();
                }
            }

            private IEnumerator BuggerTakeDamage()
            {
                _canTakeDamage = false;
                yield return new WaitForSeconds(0.5f);
                _canTakeDamage = true;
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
                PlayRezRpc();
            }
            #endregion
            
            #region Replication callbacks (every client)

            private void OnHpChanged(float previous, float current)
            {
                OnChangeHp?.Invoke(current, MaxHp);
                if(IsOwner) InvokeEvent(new PlayerHealthChanged_EVENT
                {
                    CurrentHp = current,
                    MaxHp = MaxHp,
                    Invincible =  IsInvincible
                });
            }

            private void OnInvincibleChanged(bool previous, bool current)
            {
                OnChangedInvincibility?.Invoke(current);
                if(IsOwner) InvokeEvent(new PlayerHealthChanged_EVENT
                {
                    CurrentHp = CurrentHp,
                    MaxHp = MaxHp,
                    Invincible =  IsInvincible
                });
            }

            private void OnDownedChanged(bool previous, bool downed)
            {
                OnChangedDown?.Invoke(downed);
                InvokeEvent(new DownedChanged_EVENT()
                {
                    Downed = downed,
                    PlayerId = OwnerClientId
                });
            }
            
            #endregion
            
            #region SFX
            [Rpc(SendTo.Everyone)]
            private void PlayDeathRpc() => PlaySfx(_deathSound, 0.5f);
            
            [Rpc(SendTo.Everyone)]
            private void PlayRezRpc() => PlaySfx(_rezSound, 0.5f);
            
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

            [Rpc(SendTo.Everyone)]
            private void HitMarkClientRpc()
            {
                StartCoroutine(HitMarkColor());
            }

            IEnumerator HitMarkColor()
            {
                Color previousColor = _bodyMesh.material.color;
                
                _bodyMesh.material.color = Color.white;
                
                yield return new WaitForSeconds(0.2f);
                
                _bodyMesh.material.color = previousColor;
            }
            
            #endregion
            
        }
    }