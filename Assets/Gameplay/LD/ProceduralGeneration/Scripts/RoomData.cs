using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public enum Socket { Wall = 0, Door = 1, Window = 2, None = 3 }
    public enum RoomKind { Filler, Start, Final }
    
    [Serializable]
    public class RoomCell
    {
        public Vector2Int pos;
        public Socket north, east, south, west;
 
        public Socket[] ToArray() => new[] { north, east, south, west };
    }
    
    [CreateAssetMenu(fileName = "Room",  menuName = "Generation/Room Data")]
    public class RoomData : ScriptableObject
    {
        public RoomKind kind = RoomKind.Filler;
        public GameObject prefab;
        
        public List<RoomCell> cells = new List<RoomCell> { new RoomCell() };
 
        [Min(1)] public int maxCount = 3;
        [Min(1)] public int weight = 1;
        public bool allowRotation = true;
    }
}