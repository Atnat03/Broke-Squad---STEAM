using System;
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
        [SerializeField] private Camera _cam;
        [SerializeField] private LayerMask _layerMask;
	
        [Header("Ping")]
        [SerializeField] private GameObject _pingPrefab;
        [SerializeField] private float _cooldownLifePing = 5f;
        [SerializeField] private float _timerBetweenPing = 2f;
	
        float _elapsedTime = 0f;
        private bool _canPing = true;

        private GameObject _currentPing;
	
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
            ABPrint.Print("Pinging 1 ", ABColor.Red);
            
            if (!IsOwner) return;
            
            ABPrint.Print("Pinging 2 ", ABColor.Red);

            if (!_canPing) return;
            
            ABPrint.Print("Pinging 3 ", ABColor.Red);
		
            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, 10000, _layerMask ,QueryTriggerInteraction.Ignore))
            {
                _elapsedTime = _timerBetweenPing;
                
                ABPrint.Print("Pinging 4 ", ABColor.Red);
                
                if (IsServer)
                    AddPingObserverRpc(hit.point + hit.normal * 0.1f);
                else
                {
                    AddPingServerRpc(hit.point + hit.normal * 0.1f);
                }
            }
        }

        [Rpc(SendTo.Server)]
        void AddPingServerRpc(Vector3 point)
        {
            AddPingObserverRpc(point);
        }

        [Rpc(SendTo.Everyone)]
        void AddPingObserverRpc(Vector3 pos)
        {
            ABPrint.Print("Pinging : " + pos, ABColor.Red);
            
            _currentPing = Instantiate(_pingPrefab, pos, Quaternion.identity);
            
            Destroy(_currentPing.gameObject, _cooldownLifePing);
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