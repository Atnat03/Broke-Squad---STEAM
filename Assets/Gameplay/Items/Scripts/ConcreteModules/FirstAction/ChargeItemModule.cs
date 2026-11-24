using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ChargeItemModule : ActionModule, IFirstAction, IServerAction
    {
        public override string Emoji => "🔋";
        
        [SerializeField] public int PercentGain = 10;
        [SerializeField] private float _range = 2f;

        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringName;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;

            Transform cam = Context.Camera.transform;

            if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _range)) return;

            ItemPickup pickup = hit.collider.GetComponentInParent<ItemPickup>();
            if (pickup == null) return;

            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                Target = pickup.NetworkObject
            });
            
            //SFX
            Context.Inventory.InvokeEvent(new PlaySoundEvent
            {
                Position = Context.Core.transform.position,
                Volume = _volume,
                SoundName = _stringName
            });
        }

        public void StopFirstAction() { }
        public void OnNotInHand() { }
        public void OnThrow()
        {
            
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            NetworkObject target = data.GetTarget();
            if (target == null || !target.TryGetComponent(out ItemPickup pickup)) return;

            if (Vector3.Distance(Context.Inventory.transform.position, pickup.transform.position) > _range + 2f)
                return;

            if (!CanUse()) return;

            bool charged = false;
            foreach (ElectricUseModule electric in pickup.Instance.AllModules().OfType<ElectricUseModule>().Distinct())
                charged |= electric.AddEnergy(PercentGain);

            if (charged)
                ConsumeCondition(data);
        }
    }
}