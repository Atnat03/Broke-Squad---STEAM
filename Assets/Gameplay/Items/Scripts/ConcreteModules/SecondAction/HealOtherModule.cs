using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class HealOtherModule : ItemModule, IFirstAction, IServerAction
    {
        [SerializeField] private float _healAmount = 10;
        
        public ICondition Condition { get; }

        public void StartFirstAction()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out PlayerData.PlayerData data))
            {
                Context.SendToServer(this, new OnModuleDoAction_EVENT
                {
                    Target = data.NetworkObject
                });
            }
        }

        public void StopFirstAction()
        { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            // PlayerData => PlayerHealth.Heal()
            if (data.GetTarget().TryGetComponent(out PlayerData.PlayerData playerData))
            {
                playerData.Heal(_healAmount);
            }
        }
    }
}