using System;
using Gameplay.Controller;
using Gameplay.IA;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Gameplay.Controller.PlayerInput;

namespace Gameplay.UI.Scripts
{
    public class PlayerPing : NetworkBehaviour
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private SO_ItemDataBase _dataBase;
        [SerializeField] private Camera _cam;
        [SerializeField] private LayerMask _layerMask;
	
        [Header("Ping")]
        [SerializeField] private Ping[] _pingPrefab;
        [SerializeField] private float _cooldownLifePing = 5f;
        [SerializeField] private float _timerBetweenPing = 2f;
	
        float _elapsedTime = 0f;
        private bool _canPing = true;
        
        public Action<bool> OnPinging;
	
        private void Update()
        {
            if (_elapsedTime > 0)
            {
                _elapsedTime -= Time.deltaTime;
                _canPing = false;

                if (_elapsedTime <= 0)
                {
                    _canPing = true;
                }
            }
        }

        private void AddPing()
        {
            if (!IsOwner) return;
            
            if (!_canPing) return;
            
            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, 10000, _layerMask ,QueryTriggerInteraction.Ignore))
            {
                _elapsedTime = _timerBetweenPing;

                NetworkObject n = hit.transform.GetComponent<NetworkObject>();
                
                (int, int) dataPing = GetPingPrefabFromEntity(n);

                Vector3 pos = hit.point + hit.normal * 0.1f;
                
                if (n != null)
                {
                    if (n.transform.TryGetComponent(out Collider col))
                    {
                        Bounds bounds = col.bounds;

                        pos = new Vector3(bounds.center.x, bounds.max.y + 0.1f, bounds.center.z);
                    }
                }
                
                if (IsServer)
                    AddPingObserverRpc(pos, dataPing.Item1, dataPing.Item2);
                else
                {
                    AddPingServerRpc(pos, dataPing.Item1, dataPing.Item2);
                }
            }
        }

        [Rpc(SendTo.Server)]
        void AddPingServerRpc(Vector3 point, int prefabId, int itemID = -1)
        {
            AddPingObserverRpc(point, prefabId, itemID);
        }

        [Rpc(SendTo.Everyone)]
        void AddPingObserverRpc(Vector3 pos, int prefabId, int itemID = -1)
        {
            SO_Item item = itemID != -1 ? _dataBase.GetItemData(itemID) : null;
            Sprite sprite = null;
            
            if (item != null)
            {
                sprite = item.Icon;
            }
            
            Ping p = Instantiate(_pingPrefab[prefabId], pos, Quaternion.identity);
            p.SetSprites(_playerController.PlayerColorId, _cooldownLifePing, sprite);
        }

        private (int, int) GetPingPrefabFromEntity(NetworkObject n)
        {
            if(n == null)
                return (0, -1);
            
            if (n.TryGetComponent(out ItemPickup i))
            {
                return (1, i.Instance.Data.ID);
            }
            if(n.TryGetComponent(out GuardBrain g))
            {
                return (2, -1);
            }
            
            return (0, -1);
        }
	
        private void OnEnable()
        {
            _playerInput.OnPinging += AddPing;
        }

        private void On()
        {
            _playerInput.OnPinging -= AddPing;
        }
    }
}