using Gameplay.Controller;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Gameplay.LD
{
    public struct LDActivation : INetworkSerializable
    {
        public bool HasInstigator;
        public ulong InstigatorClientId;
        public ulong InstigatorObjectId;
        public Vector3 Position;
        public FixedString32Bytes TriggerPlayerName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref InstigatorClientId);
            serializer.SerializeValue(ref InstigatorObjectId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref HasInstigator); 
            serializer.SerializeValue(ref TriggerPlayerName); 
        }

        public LDActivation(NetworkObject instigator, Vector3 position, string playerNameTrigger = null)
        {
            HasInstigator = instigator != null;
            InstigatorClientId = instigator != null ? instigator.OwnerClientId : 0;
            InstigatorObjectId = instigator != null ? instigator.NetworkObjectId : 0;
            Position = position;
            TriggerPlayerName = new FixedString32Bytes(playerNameTrigger);
        }

        public bool TryGetInstigator(out NetworkObject instigator)
        {
            instigator = null;
            if (!HasInstigator || NetworkManager.Singleton == null) return false;
            return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(InstigatorObjectId, out instigator);
        }
    }
}