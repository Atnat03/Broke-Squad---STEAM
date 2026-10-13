using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public struct OnPickupItem_EVENT
    {
        public ulong ClientId;
        public NetworkObjectReference ItemPickUpRef;
    }
    
    public struct OnGrabGoal_EVENT{}
    public struct OnDropGoal_EVENT{}
    
    public struct OnModuleDoAction_EVENT : INetworkSerializable
    {
        public ulong ClientId;
        public FixedString32Bytes KeyEvent;
        public NetworkObjectReference Target;
        public float ValueF;
        public int ValueI;
        public bool ValueB;
        public FixedString32Bytes ValueS;
        public Vector3 Position;
        public Quaternion Rotation;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Target);
            serializer.SerializeValue(ref KeyEvent);
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref ValueF);
            serializer.SerializeValue(ref ValueI);
            serializer.SerializeValue(ref ValueB);
            serializer.SerializeValue(ref ValueS);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }

        public NetworkObject GetTarget()
        {
            if (Target.TryGet(out NetworkObject networkObject))
            {
                return networkObject;
            }
            
            return null;
        }
    }
}