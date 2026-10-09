using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class ManorBuilder : MonoBehaviour
    {
        public RoomData[] pool;

        [Header("Grid")]
        public int width = 4;
        public int height = 4;
        public float cellSize = 10f;
        public Vector2Int startOrigin = Vector2Int.zero;

        [Header("Contraintes")]
        public int minCellDistance = 4;
        public int minRoomDistance = 3;
        public int maxVoidCells = 16;
        [Range(0, 100)] public int voidPercent = 30;

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
                width = width, height = height,
                startOrigin = startOrigin,
                minCellDistance = minCellDistance,
                minRoomDistance = minRoomDistance,
                maxVoidCells = maxVoidCells,
                voidPercent = voidPercent
            };

            Result = gen.Generate(seed, pool);
            if (Result == null)
            {
                Debug.LogError("Mansion: aucune map valide. Ajoute des fillers (toutes combinaisons de portes) ou assouplis les contraintes.");
                return;
            }

            foreach (var room in Result.rooms)
            {
                if (room.variant.data.prefab == null) continue;

                var c = room.PivotCell;
                var worldPos = transform.position + new Vector3(c.x * cellSize, 0f, -c.y * cellSize);
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
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Gizmos.DrawWireCube(transform.position + new Vector3(x * cellSize, 0, -y * cellSize),
                    new Vector3(cellSize, 0.1f, cellSize));
        }
    }
}