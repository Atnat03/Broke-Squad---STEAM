using System;
using Bus;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;
using Vehicle;

namespace Gameplay.Item
{
    [RequireComponent(typeof(VehicleBehavior))]
    public class RcCarItemBrigde : NetworkBusListener
    {
        private string _keyEvent = "RC_CAR_EVENT";
        [SerializeField] private int _carID = 0;

        VehicleBehavior _vehicleBehavior;

        private void Awake() => _vehicleBehavior = GetComponent<VehicleBehavior>();

        public override void OnNetworkSpawn()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateRcCarState);
        }

        private void UpdateRcCarState(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _keyEvent) return;
            if (data.ValueI != _carID) return;

            if (!IsServer)
            {
                if (data.ClientId != NetworkManager.LocalClientId) return;
                TryActivateCarServerRpc(data);
                return;
            }

            if (data.ValueB)
            {
                if (_vehicleBehavior.DriverId != VehicleBehavior.NoDriver &&
                    _vehicleBehavior.DriverId != data.ClientId) return;

                _vehicleBehavior.SetDriver(data.ClientId);
            }
            else if (_vehicleBehavior.DriverId == data.ClientId)
            {
                _vehicleBehavior.SetDriver(VehicleBehavior.NoDriver);
            }
        }

        [Rpc(SendTo.Server)]
        private void TryActivateCarServerRpc(OnModuleDoAction_EVENT data) => UpdateRcCarState(data);
    }
}
