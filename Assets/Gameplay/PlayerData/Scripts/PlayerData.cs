    using System;
    using Bus;
    using Gameplay.LD.Scripts;
    using Unity.Netcode;
    using UnityEngine;

    namespace Gameplay.PlayerData
    {
        public class PlayerData : NetworkBusListener, IDamageable
        {
            public const int MaxHpValue = 100;

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
            
            public int CurrentHp => _playerHp.Value;
            public int MaxHp => MaxHpValue;

            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                ListenToEvent<PlayerRevivedEvent>(Revive);
                _playerHp.OnValueChanged += OnHpChanged;
            }

            public override void OnNetworkDespawn()
            {
                _playerHp.OnValueChanged -= OnHpChanged;

                base.OnNetworkDespawn();
            }

            [ContextMenu("Test")]
            public void Test()
            {
                ApplyDamage(10);
            }
            
            public void ApplyDamage(float damage)
            {
                if (!IsServer || damage <= 0 || _playerDead.Value)
                    return;

                _playerHp.Value = Mathf.Max(0, _playerHp.Value - (int)damage);

                if (_playerHp.Value <= 0)
                    SetPlayerDead(true);
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
                    NotifyDeathOwnerRpc();
            }
            
            private void OnHpChanged(int previousHp, int newHp)
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
            }

            private void Revive(PlayerRevivedEvent e)
            {
                _playerHp.Value = MaxHp;
                _playerDead.Value = false;
            }
            
            [Rpc(SendTo.Owner)]
            private void NotifyDeathOwnerRpc()
            {
                InvokeEvent(new PlayerDeathEvent
                {
                    playerID = OwnerClientId
                });
                
                
            }
        }
    }