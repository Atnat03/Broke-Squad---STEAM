using System;
using System.Collections.Generic;
using Bus;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;

namespace Gameplay.Other
{
    public class GameManager : NetworkBusListener
    {
        private bool _hasGoal = false;
        
        private readonly NetworkVariable<bool> _win = new();
        private readonly NetworkVariable<bool> _loose = new();
        
        public override void OnNetworkSpawn()
        {
            _win.OnValueChanged += OnWin;
            _loose.OnValueChanged += OnLoose;
            
            ListenToEvent<OnTryToWin_EVENT>(TryToWin);

            if (IsServer)
            {
                ListenToEvent<OnGrabGoal_EVENT>(SetHasGoal);
                ListenToEvent<OnDropGoal_EVENT>(SetDropGoal);
            }
        }

        private void SetHasGoal(OnGrabGoal_EVENT data)
        {
            _hasGoal = true;   
        }
        private void SetDropGoal(OnDropGoal_EVENT data) => _hasGoal = false;
        
        private void OnWin(bool previousValue, bool newValue) { }
        private void OnLoose(bool previousValue, bool newValue) { }

        void TryToWin(OnTryToWin_EVENT data)
        {
            TryWin();
        }
        
        void TryWin()
        {
            if (!IsServer)
            {
                TryWinRpc();
                return;
            }

            ABPrint.Print("Try to win");
            
            if (_hasGoal)
            {
                ABPrint.Print("WIN !!", ABColor.Green);
            }
            else
            {
                ABPrint.Print("U cant win, u dont have the goal !!", ABColor.Red);
            }
        }

        [Rpc(SendTo.Server)]
        private void TryWinRpc() => TryWin();
    }
}