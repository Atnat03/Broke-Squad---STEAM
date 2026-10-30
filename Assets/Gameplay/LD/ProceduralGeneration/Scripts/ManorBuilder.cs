using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class ManorBuilder : MonoBehaviour
    {
        public RoomData[] pool;
        public Dictionary<int, Vector3> RoomOffsets { get; } = new Dictionary<int, Vector3>();
        
        [Header("Grid")]
        public int floorCount = 2;
        public int maxExtent = 8;
        public float cellSize = 10f;
        public float floorHeight = 4f;
        public Vector2Int startOrigin = Vector2Int.zero;

        [Header("Constraint")]
        public int targetRooms = 20;
        public int minRooms = 12;
        public int minRoomsPerFloor = 1;
        public int minCellDistance = 4;
        public int minRoomDistance = 3;

        [Header("Solver")]
        public int maxAttempts = 50;
        public int solverBudget = 2000;
        public int maxMillis = 2000;

        [Header("Debug")]
        public bool buildOnStart = true;
        public int debugSeed = 12345;

        public MapResult Result { get; private set; }

        void Start()
        {
            if (buildOnStart) Build(debugSeed);
        }

        [ContextMenu("Build")]
        public void DebugSeed()
        {
            Build(debugSeed);
        }
        
        public void Build(int seed)
        {
            Clear();
            RoomOffsets.Clear();

            MapGenerator generator = new MapGenerator();
            generator.floorCount = floorCount;
            generator.maxExtent = maxExtent;
            generator.startOrigin = startOrigin;
            generator.targetRooms = targetRooms;
            generator.minRooms = minRooms;
            generator.minRoomsPerFloor = minRoomsPerFloor;
            generator.minCellDistance = minCellDistance;
            generator.minRoomDistance = minRoomDistance;
            generator.maxAttempts = maxAttempts;
            generator.solverBudget = solverBudget;
            generator.maxMillis = maxMillis;

            Result = generator.Generate(seed, pool);
            if (Result == null)
            {
                Debug.LogError("Mansion: No Valid map or used to much time to generate mansion");
                return;
            }

            foreach (PlacedRoom room in Result.rooms)
            {
                RoomData roomData = room.variant.roomData;
                if (roomData.prefab == null) continue;

                Vector3Int pivotCell = room.PivotCell;
                Vector3 worldPosition = transform.position + new Vector3(pivotCell.x * cellSize, pivotCell.z * floorHeight, -pivotCell.y * cellSize);
                Quaternion worldRotation = transform.rotation * Quaternion.Euler(0f, 90f * room.variant.rotationIndex, 0f);

                GameObject instance = Instantiate(roomData.prefab, worldPosition, worldRotation, transform);
                instance.name = roomData.name + "_" + room.id;
                Vector3 alignmentOffset = AlignToFootprint(instance, room);
                RoomOffsets[room.id] = alignmentOffset;
            }

            Debug.Log("Mansion seed " + seed + " : " + Result.rooms.Count + " rooms, path start->final = " + Result.pathLength + " rooms, " + Result.attempts + " tries.");
        }
        
        Vector3 AlignToFootprint(GameObject instance, PlacedRoom room)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Vector3.zero;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3Int minCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int maxCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            for (int i = 0; i < room.variant.cellPositions.Length; i++)
            {
                Vector3Int cell = room.variant.cellPositions[i] + room.origin;
                minCell = Vector3Int.Min(minCell, cell);
                maxCell = Vector3Int.Max(maxCell, cell);
            }

            Vector3 targetPosition = transform.position + new Vector3(
                (minCell.x + maxCell.x) * 0.5f * cellSize,
                minCell.z * floorHeight,
                -(minCell.y + maxCell.y) * 0.5f * cellSize);

            Vector3 offset = new Vector3(
                targetPosition.x - bounds.center.x,
                targetPosition.y - bounds.min.y,
                targetPosition.z - bounds.center.z);

            instance.transform.position += offset;
            return offset;
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        
        public Vector3 CellToWorld(Vector3Int cell)
        {
            return transform.position + new Vector3(cell.x * cellSize, cell.z * floorHeight, -cell.y * cellSize);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            for (int z = 0; z < floorCount; z++)
            {
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(startOrigin.x * cellSize, z * floorHeight, -startOrigin.y * cellSize),
                    new Vector3(cellSize, 0.1f, cellSize));
            }

            if (Result == null) return;

            Gizmos.color = Color.cyan;
            foreach (PlacedRoom room in Result.rooms)
            {
                for (int i = 0; i < room.variant.cellPositions.Length; i++)
                {
                    Vector3Int cell = room.variant.cellPositions[i] + room.origin;
                    Gizmos.DrawWireCube(transform.position + new Vector3(cell.x * cellSize, cell.z * floorHeight, -cell.y * cellSize),
                        new Vector3(cellSize, 0.1f, cellSize));
                }
            }
        }
    }
}