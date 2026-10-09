using System;
using System.Collections;
using Bus;
using Gameplay.IA.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Other.SuspiciousSound
{
    public class SS_SenderTest : NetworkBusListener
    {
        [SerializeField] private SO_SuspiciousSoundSettings _settingsSound;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                GenerateSuspiciousSound();
            }
        }

        public void GenerateSuspiciousSound()
        {
            if (!IsServer)
            {
                GenerateSSRpc();
                return;
            }

            Generate();
        }

        [Rpc(SendTo.Server)]
        private void GenerateSSRpc() => GenerateSuspiciousSound();
        
        private void Generate()
        {
            InvokeEvent(new OnCreateSuspiciousSound_EVENT
            {
                FromClientID = OwnerClientId,
                Force = _settingsSound.Force,
                MaxDistance = _settingsSound.MaxDistance,
            });
        }
    }
}