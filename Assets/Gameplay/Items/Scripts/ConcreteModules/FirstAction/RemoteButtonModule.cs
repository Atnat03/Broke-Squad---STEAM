using Unity.Netcode;
using UnityEngine;

public enum RemoteButtonPhase { Press, HoldStart, HoldEnd }
public struct OnRemoteAction_EVENT
{
    public string RemoteId;
    public RemoteButtonPhase Phase;
    public ulong ClientId;
    public float ActiveDuration;
}

namespace Gameplay.Items
{
    public class RemoteButtonModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] string remoteId;
        [SerializeField] bool holdMode = false;
        [SerializeField] float activeDuration = 5f;
        
        public ICondition Condition { get; }
        
        bool _pressedLocally;
        
        bool _holding;
        ulong _holder;
        
        
        public void StartFirstAction()
        {
            _pressedLocally = true;
            Context.SendToServer(this, new OnModuleDoAction_EVENT { ValueB = true});
        }

        public void StopFirstAction()
        {
            if (!_pressedLocally) return;
            _pressedLocally = false;
            Context.SendToServer(this, new OnModuleDoAction_EVENT { ValueB = false });
        }

        public void OnNotInHand() => StopFirstAction();
        public void OnThrow() => StopFirstAction();

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            ulong sender = NetworkManager.Singleton.LocalClientId;

            if (!holdMode)
            {
                if (data.ValueB) Publish(RemoteButtonPhase.Press, sender);
                return;
            }

            if (data.ValueB)
            {
                if (_holding) return;
                _holding = true;
                _holder = sender;
                Publish(RemoteButtonPhase.HoldStart, sender);
            }
            else
            {
                ServerRelease();
            }
        }

        void ServerRelease()
        {
            if (!_holding) return;
            _holding = false;
            Publish(RemoteButtonPhase.HoldEnd, _holder);
        }

        void Publish(RemoteButtonPhase phase, ulong clientId)
        {
            Context.Inventory.InvokeEvent(new OnRemoteAction_EVENT
            {
                RemoteId = remoteId,
                Phase = phase,
                ClientId = clientId,
                ActiveDuration = activeDuration
            });
        }
    }
    
}