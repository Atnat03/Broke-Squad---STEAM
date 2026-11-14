using System;
using Bus;
using Gameplay.Items;
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

        private void Awake()
        {
            _vehicleBehavior = GetComponent<VehicleBehavior>();
            _vehicleBehavior.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateRcCarState);
        }

        private void UpdateRcCarState(OnModuleDoAction_EVENT data)
        {
            if (!IsServer) return;

            if (data.KeyEvent != _keyEvent) return;
            if(data.ValueI != _carID) return;

            _vehicleBehavior.enabled = data.ValueB;
        }
    }
}
