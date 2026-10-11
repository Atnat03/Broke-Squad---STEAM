using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ChargeItemModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] public int _percentGain = 10;
        [SerializeField] private float _range = 2f;

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
        }

        public void StopFirstAction() { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            NetworkObject target = data.GetTarget();
            if (target == null || !target.TryGetComponent(out ItemPickup pickup)) return;

            if (Vector3.Distance(Context.Inventory.transform.position, pickup.transform.position) > _range + 2f)
                return;

            if (!CanUse()) return;

            bool charged = false;
            foreach (ElectricUseModule electric in pickup.Instance.AllModules().OfType<ElectricUseModule>().Distinct())
                charged |= electric.AddEnergy(_percentGain);

            if (charged)
                ConsumeCondition(data);
        }
    }
}