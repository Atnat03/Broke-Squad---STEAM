using UnityEngine;

namespace Gameplay.Items
{
    public class PlugDamageModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] private float damage;
        public ICondition Condition { get; }
        public void StartFirstAction()
        {
            Transform cam = Context.Camera.transform;
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f, LayerMask.GetMask("ElectricPlug")))
            {
                Context.SendToServer(this, new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        {
        }

        public void OnNotInHand()
        {
        }

        public void OnThrow()
        {
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            Context.Inventory.Controller.PlayerHealth.ApplyDamage(damage);
        }
    }
}