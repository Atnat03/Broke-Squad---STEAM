using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Items
{
    [Serializable]
    public class DrunkEffectWhenUseModule : ActionModule, IFirstAction
    {
        [SerializeField] private float _drunkDuration = 5f;
        private int _rendererIndex = 1;
        
        [SerializeField, SoundName] private string _sfxName;
        [SerializeField, SoundName] private float _volume = 0.5f;

        private Coroutine _drunkCoroutine;
        
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            if (_drunkCoroutine != null)
            {
                Context.Inventory.StopCoroutine(_drunkCoroutine);
            }
            _drunkCoroutine = Context.Inventory.StartCoroutine(DrunkRoutine());
        }

        public void StopFirstAction() { }

        private IEnumerator DrunkRoutine()
        {
            var renderer = Context.Camera.GetUniversalAdditionalCameraData();
            
            renderer.SetRenderer(_rendererIndex);
            
            Context.Inventory.InvokeEvent(new PlaySoundEvent
            {
                position = Context.Core.transform.position,
                volume = _volume,
                soundName = _sfxName
            });
            
            yield return new WaitForSeconds(_drunkDuration);

            renderer.SetRenderer(0);
        }
    }
}