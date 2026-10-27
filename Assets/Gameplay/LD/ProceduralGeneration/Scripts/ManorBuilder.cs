using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class ManorBuilder : MonoBehaviour
    {
        public RoomData[] pool;

        [Header("Grid")]
        public int floorCount = 2;
        public int maxExtent = 8;
        public float cellSize = 10f;
        public float floorHeight = 4f;
        public Vector2Int startOrigin = Vector2Int.zero;

        [Header("Contraintes")]
        public int targetRooms = 20;
        public int minRooms = 12;
        public int minRoomsPerFloor = 1;
        public int minCellDistance = 4;
        public int minRoomDistance = 3;

        [Header("Debug")]
        public bool buildOnStart = true;
        public int debugSeed = 12345;

        public MapResult Result { get; private set; }

        void Start()
        {
            if (buildOnStart) Build();
        }

        [ContextMenu("Build")]
        public void Build()
        {
            int seed = debugSeed;
            Clear();

            var gen = new MapGenerator()
            {
                floorCount = floorCount,
                maxExtent = maxExtent,
                startOrigin = startOrigin,
                targetRooms = targetRooms,
                minRooms = minRooms,
                minRoomsPerFloor = minRoomsPerFloor,
                minCellDistance = minCellDistance,
                minRoomDistance = minRoomDistance
            };

            Result = gen.Generate(seed, pool);
            if (Result == null)
            {
                Debug.LogError("Mansion: aucune map valide. Ajoute des dead ends et des rooms à 2-3 portes ou assouplis les contraintes.");
                return;
            }

            foreach (var room in Result.rooms)
            {
                if (room.variant.data.prefab == null) continue;

                var c = room.PivotCell;
                var worldPos = transform.position + new Vector3(c.x * cellSize, c.z * floorHeight, -c.y * cellSize);
                var worldRot = transform.rotation * Quaternion.Euler(0f, 90f * room.variant.rotation, 0f);

                var go = Instantiate(room.variant.data.prefab, worldPos, worldRot, transform);
                go.name = $"{room.variant.data.name}_{room.id}";
            }

            Debug.Log($"Mansion seed {seed} : {Result.rooms.Count} rooms, chemin start->final = {Result.pathLength} rooms, {Result.attempts} tentative(s).");
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            for (int z = 0; z < floorCount; z++)
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(startOrigin.x * cellSize, z * floorHeight, -startOrigin.y * cellSize),
                    new Vector3(cellSize, 0.1f, cellSize));

            if (Result == null) return;
            Gizmos.color = Color.cyan;
            foreach (var room in Result.rooms)
                foreach (var p in room.variant.pos)
                {
                    var c = p + room.origin;
                    Gizmos.DrawWireCube(
                        transform.position + new Vector3(c.x * cellSize, c.z * floorHeight, -c.y * cellSize),
                        new Vector3(cellSize, 0.1f, cellSize));
                }
        }
    }
}