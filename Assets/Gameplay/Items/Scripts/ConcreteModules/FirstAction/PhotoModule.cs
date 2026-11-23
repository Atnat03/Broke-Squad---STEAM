using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class PhotoModule : ActionModule, IFirstAction, IReplicatedByteModule
    {
        private byte[] _photoData = Array.Empty<byte>();
        
        public int Version { get; private set; }

        
        public ICondition Condition => ConditionParent;

        public override string Emoji => "📷";

        public void SetPhotoData(byte[] data)
        {
            _photoData = data != null
                ? (byte[])data.Clone()
                : Array.Empty<byte>();

            Version++;
        }


        public byte[] GetByteState()
        {
            return _photoData != null
                ? (byte[])_photoData.Clone()
                : Array.Empty<byte>();
        }

        public void SetByteState(byte[] data)
        {
            SetPhotoData(data);
        }

        protected override void OnCloned()
        {
            base.OnCloned();
            _photoData = _photoData != null
                ? (byte[])_photoData.Clone()
                : Array.Empty<byte>();
            Version++;
        }

        public void StartFirstAction() { }
        public void StopFirstAction() { }
        public void OnNotInHand() { }
        public void OnThrow() { }
    }
    
    public struct PhotoChunkState : INetworkSerializable, IEquatable<PhotoChunkState>
    {
        public int ModuleIndex;
        public int ChunkIndex;
        public int ChunkCount;
        public FixedString512Bytes Data;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ModuleIndex);
            serializer.SerializeValue(ref ChunkIndex);
            serializer.SerializeValue(ref ChunkCount);
            serializer.SerializeValue(ref Data);
        }

        public bool Equals(PhotoChunkState other)
        {
            return ModuleIndex == other.ModuleIndex
                   && ChunkIndex == other.ChunkIndex
                   && ChunkCount == other.ChunkCount
                   && Data.Equals(other.Data);
        }
    }
}