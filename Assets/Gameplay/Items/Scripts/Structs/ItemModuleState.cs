using UnityEngine;
using System;
using Unity.Collections;
using Unity.Netcode;

namespace Gameplay.Items
{
    public struct ItemModuleState : INetworkSerializable, IEquatable<ItemModuleState>
    {
        public int ModuleIndex;
        public int Value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ModuleIndex);
            serializer.SerializeValue(ref Value);
        }

        public bool Equals(ItemModuleState other) => ModuleIndex == other.ModuleIndex && Value == other.Value;
    }
}