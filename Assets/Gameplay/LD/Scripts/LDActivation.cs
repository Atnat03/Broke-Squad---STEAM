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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref InstigatorClientId);
            serializer.SerializeValue(ref InstigatorObjectId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref HasInstigator);
        }

        public LDActivation(NetworkObject instigator, Vector3 position)
        {
            HasInstigator = instigator != null;
            InstigatorClientId = instigator != null ? instigator.OwnerClientId : 0;
            InstigatorObjectId = instigator != null ? instigator.NetworkObjectId : 0;
            Position = position;
        }

        public bool TryGetInstigator(out NetworkObject instigator)
        {
            instigator = null;
            if (!HasInstigator || NetworkManager.Singleton == null) return false;
            return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(InstigatorObjectId, out instigator);
        }
    }
}