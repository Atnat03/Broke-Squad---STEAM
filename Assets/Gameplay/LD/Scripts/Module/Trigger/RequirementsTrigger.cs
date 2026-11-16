using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public enum FireOn
    {
        AllActivated,
        AnyDeactivated
    }

    public class RequirementsTrigger : LDTriggerModule
    {
        [Tooltip("Les LD surveillés")]
        [SerializeField] private LDElement[] _required;

        [SerializeField] private LDFlag _flag = LDFlag.Activated;

        [Tooltip("AllActivated : se déclenche quand tous les éléments sont activés. AnyDeactivated : se déclenche quand l'un d'eux est désactivé.")]
        [SerializeField] private FireOn _fireOn = FireOn.AllActivated;

        private bool _allMet;

        protected override void OnInitialize()
        {
            if (!Context.IsServer) return;
            foreach(var element in _required)
            {
                if (element != null)
                    element.FlagsChanged += OnRequirementChanged;
            }
        }

        public override void OnDestroy()
        {
            foreach (var element in _required)
                if (element != null) element.FlagsChanged -= OnRequirementChanged;
        }

        private void OnRequirementChanged(LDFlag flag)
        {
            bool allMet = true;
            foreach (var element in _required)
                if (element == null || !element.HasFlag(_flag)) { allMet = false; break; }

            if (allMet == _allMet) return;
            _allMet = allMet;

            if (allMet == (_fireOn == FireOn.AllActivated))
                Raise(new LDActivation(null, transform.position));
        }
    }
}
