using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class PlaySoundModule : ActionModule, IFirstAction
    {
        public override string Emoji => "👂";

        private string _key = "PLAY_SOUND_EVENT";
        
        [Header("Sound")]
        [SerializeField][SoundName] private string _soundName;
        [SerializeField] private float _volume = 0.5f;
        
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            if (!CanUse()) return;

            ABPrint.Print("Trigger Sound", ABColor.White);
            
            Context.SendEventToEveryone(new OnModuleDoAction_EVENT
            {
                Position = Context.Core.transform.position,
                KeyEvent =  _key,
                ValueF = _volume,
                ValueS = _soundName,
                InstanceId = Context.Core.Instance.InstanceId
            });
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction()
        { }

        public void OnNotInHand()
        { }

        public void OnThrow()
        { }
    }
}