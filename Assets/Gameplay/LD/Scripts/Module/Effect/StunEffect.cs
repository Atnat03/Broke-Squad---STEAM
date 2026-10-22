using Gameplay.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class StunEffect : LDEffectModule
    {
        [SerializeField] private float _stunDuration = 1;

        public override void Activate(LDActivation activation)
        {
            if(!activation.TryGetInstigator(out var instigator))
            {
                Debug.LogWarning($"[LD] Activation without instigator", this);
                return;
            }

            PlayerController player = instigator.GetComponent<PlayerController>();
            if(player == null)
            {
                Debug.LogWarning($"[LD] {instigator.name} Stun effect");
                return;
            }

            player.Stun(_stunDuration);
        }
    }
}
