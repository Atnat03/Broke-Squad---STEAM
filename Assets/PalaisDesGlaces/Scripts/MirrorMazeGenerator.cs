using System.Collections.Generic;
using UnityEngine;

namespace PalaisDesGlaces
{
    /// <summary>
    /// Génère un labyrinthe de miroirs (algorithme "recursive backtracker" + quelques ouvertures
    /// en plus pour créer des boucles). Les murs alignés sont fusionnés pour limiter le nombre
    /// de miroirs. Clic droit sur le composant > "Générer le labyrinthe" pour régénérer.
    /// </summary>
    public class MirrorMazeGenerator : MonoBehaviour
    {
        [Header("Labyrinthe")]
        public int width = 7;
        public int depth = 7;
        public float cellSize = 3f;
        public float wallHeight = 3f;
        public float wallThickness = 0.2f;
        public int seed = 1234;
        [Range(0f, 0.5f)] public float extraOpenings = 0.12f;

        [Header("Matériaux")]
        public Material mirrorMaterial;
        public Material frameMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;
        public Material goalMaterial;
        public Material[] orbMaterials;
        [Tooltip("Cubemap du reflet de secours. Vide = probe en mode Baked (à re-baker après génération).")]
        public Texture fallbackReflection;

        [Header("Décor")]
        [Range(0f, 1f)] public float orbChance = 0.18f;
        public Color[] lightColors = { new Color(1f, 0.85f, 0.7f), new Color(0.75f, 0.85f, 1f), new Color(1f, 0.7f, 0.9f) };
        public float lightIntensity = 2.5f;

        [Header("Reflets de secours")]
        [Tooltip("Une reflection probe par case : les miroirs sans vrai reflet montrent le bon couloir (flou) au lieu d'un mur sombre.")]
        public bool perCellProbes = true;
        public int probeResolution = 128;

        [Header("Joueur")]
        public Transform player;

        const string RootName = "Generated";

        bool[,] _h; // _h[x, z] : mur horizontal (le long de X) sur la ligne z, cellule x. z dans [0, depth]
        bool[,] _v; // _v[x, z] : mur vertical (le long de Z) sur la ligne x, cellule z. x dans [0, width]

        [ContextMenu("Générer le labyrinthe")]
        public void Generate()
        {
            ClearGenerated();
            var root = new GameObject(RootName).transform;
            root.SetParent(transform, false);

            var rng = new System.Random(seed);
            CarveMaze(rng);

            BuildFloorAndCeiling(root);
            BuildWalls(root);
            Vector2Int goal = FarthestCell(new Vector2Int(0, 0));
            BuildDecor(root, rng, goal);
            BuildReflectionProbe(root);

            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = transform.TransformPoint(CellCenter(0, 0));
                player.rotation = transform.rotation;
                if (cc != null) cc.enabled = true;
            }
        }

        [ContextMenu("Effacer le labyrinthe")]
        public void ClearGenerated()
        {
            var existing = transform.Find(RootName);
            while (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
                existing = Application.isPlaying ? null : transform.Find(RootName);
            }
        }

        Vector3 CellCenter(int x, int z) => new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);

        // ---------- Génération ----------

        void CarveMaze(System.Random rng)
        {
            _h = new bool[width, depth + 1];
            _v = new bool[width + 1, depth];
            for (int x = 0; x < width; x++) for (int z = 0; z <= depth; z++) _h[x, z] = true;
            for (int x = 0; x <= width; x++) for (int z = 0; z < depth; z++) _v[x, z] = true;

            var visited = new bool[width, depth];
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(0, 0));
            visited[0, 0] = true;
            var neighbours = new List<Vector2Int>(4);

            while (stack.Count > 0)
            {
                var c = stack.Peek();
                neighbours.Clear();
                if (c.x > 0 && !visited[c.x - 1, c.y]) neighbours.Add(new Vector2Int(c.x - 1, c.y));
                if (c.x < width - 1 && !visited[c.x + 1, c.y]) neighbours.Add(new Vector2Int(c.x + 1, c.y));
                if (c.y > 0 && !visited[c.x, c.y - 1]) neighbours.Add(new Vector2Int(c.x, c.y - 1));
                if (c.y < depth - 1 && !visited[c.x, c.y + 1]) neighbours.Add(new Vector2Int(c.x, c.y + 1));

                if (neighbours.Count == 0) { stack.Pop(); continue; }

                var n = neighbours[rng.Next(neighbours.Count)];
                RemoveWallBetween(c, n);
                visited[n.x, n.y] = true;
                stack.Push(n);
            }

            // Ouvertures supplémentaires (murs intérieurs uniquement) -> boucles
            for (int x = 0; x < width; x++)
                for (int z = 1; z < depth; z++)
                    if (_h[x, z] && rng.NextDouble() < extraOpenings) _h[x, z] = false;
            for (int x = 1; x < width; x++)
                for (int z = 0; z < depth; z++)
                    if (_v[x, z] && rng.NextDouble() < extraOpenings) _v[x, z] = false;
        }

        void RemoveWallBetween(Vector2Int a, Vector2Int b)
        {
            if (a.x != b.x) _v[Mathf.Max(a.x, b.x), a.y] = false;
            else _h[a.x, Mathf.Max(a.y, b.y)] = false;
        }

        Vector2Int FarthestCell(Vector2Int start)
        {
            var dist = new int[width, depth];
            for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++) dist[x, z] = -1;
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            dist[start.x, start.y] = 0;
            Vector2Int far = start;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (dist[c.x, c.y] > dist[far.x, far.y]) far = c;
                void Try(int nx, int nz, bool blocked)
                {
                    if (blocked || nx < 0 || nz < 0 || nx >= width || nz >= depth || dist[nx, nz] >= 0) return;
                    dist[nx, nz] = dist[c.x, c.y] + 1;
                    q.Enqueue(new Vector2Int(nx, nz));
                }
                Try(c.x - 1, c.y, _v[c.x, c.y]);
                Try(c.x + 1, c.y, _v[c.x + 1, c.y]);
                Try(c.x, c.y - 1, _h[c.x, c.y]);
                Try(c.x, c.y + 1, _h[c.x, c.y + 1]);
            }
            return far;
        }

        // ---------- Construction ----------

        void BuildFloorAndCeiling(Transform root)
        {
            float sx = width * cellSize, sz = depth * cellSize;
            var floor = CreateCube("Sol", root, new Vector3(sx * 0.5f, -0.1f, sz * 0.5f), new Vector3(sx + 1f, 0.2f, sz + 1f), floorMaterial);
            floor.isStatic = true;
            var ceiling = CreateCube("Plafond", root, new Vector3(sx * 0.5f, wallHeight + 0.1f, sz * 0.5f), new Vector3(sx + 1f, 0.2f, sz + 1f), ceilingMaterial);
            ceiling.isStatic = true;
        }

        void BuildWalls(Transform root)
        {
            var walls = new GameObject("Murs").transform;
            walls.SetParent(root, false);

            // Murs horizontaux (le long de X)
            for (int z = 0; z <= depth; z++)
            {
                int x = 0;
                while (x < width)
                {
                    if (!_h[x, z]) { x++; continue; }
                    int start = x;
                    while (x < width && _h[x, z]) x++;
                    float a = start * cellSize, b = x * cellSize;
                    BuildWallSegment(walls, new Vector3((a + b) * 0.5f, 0f, z * cellSize), b - a, alongX: true,
                        mirrorPositiveSide: z < depth, mirrorNegativeSide: z > 0);
                }
            }

            // Murs verticaux (le long de Z)
            for (int xl = 0; xl <= width; xl++)
            {
                int z = 0;
                while (z < depth)
                {
                    if (!_v[xl, z]) { z++; continue; }
                    int start = z;
                    while (z < depth && _v[xl, z]) z++;
                    float a = start * cellSize, b = z * cellSize;
                    BuildWallSegment(walls, new Vector3(xl * cellSize, 0f, (a + b) * 0.5f), b - a, alongX: false,
                        mirrorPositiveSide: xl < width, mirrorNegativeSide: xl > 0);
                }
            }
        }

        void BuildWallSegment(Transform parent, Vector3 center, float length, bool alongX, bool mirrorPositiveSide, bool mirrorNegativeSide)
        {
            var wall = new GameObject(alongX ? "Mur_X" : "Mur_Z").transform;
            wall.SetParent(parent, false);
            wall.localPosition = center;
            wall.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);

            // Cadre / structure (porte le collider). Un peu plus long pour boucher les jonctions.
            var core = CreateCube("Cadre", wall, new Vector3(0f, wallHeight * 0.5f, 0f),
                new Vector3(length + wallThickness + 0.02f, wallHeight, wallThickness), frameMaterial);
            core.isStatic = true;

            const float frameMargin = 0.12f;
            var mirrorScale = new Vector3(length - wallThickness - 0.04f, wallHeight - 2f * frameMargin, 1f);
            float offset = wallThickness * 0.5f + 0.003f;

            // En local du mur, le mur s'étend le long de X ; ses faces regardent +Z et -Z.
            // Un Quad regarde vers son -Z local : rotation 180° pour la face +Z.
            if (mirrorPositiveSide)
                CreateMirror(wall, new Vector3(0f, wallHeight * 0.5f, offset), Quaternion.Euler(0f, 180f, 0f), mirrorScale);
            if (mirrorNegativeSide)
                CreateMirror(wall, new Vector3(0f, wallHeight * 0.5f, -offset), Quaternion.identity, mirrorScale);
        }

        void CreateMirror(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Miroir";
            DestroyCollider(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mirrorMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<PlanarMirror>();
        }

        void BuildDecor(Transform root, System.Random rng, Vector2Int goal)
        {
            var lights = new GameObject("Lumieres").transform;
            lights.SetParent(root, false);
            int li = 0;
            for (int x = 0; x < width; x += 2)
                for (int z = 0; z < depth; z += 2)
                {
                    var go = new GameObject("Lumiere");
                    go.transform.SetParent(lights, false);
                    go.transform.localPosition = CellCenter(x, z) + Vector3.up * (wallHeight - 0.4f);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = cellSize * 3f;
                    l.intensity = lightIntensity;
                    l.color = lightColors.Length > 0 ? lightColors[li++ % lightColors.Length] : Color.white;
                    l.shadows = LightShadows.None;
                }

            var deco = new GameObject("Decor").transform;
            deco.SetParent(root, false);

            if (orbMaterials != null && orbMaterials.Length > 0)
            {
                for (int x = 0; x < width; x++)
                    for (int z = 0; z < depth; z++)
                    {
                        if ((x == 0 && z == 0) || (x == goal.x && z == goal.y)) continue;
                        if (rng.NextDouble() > orbChance) continue;
                        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        orb.name = "Orbe";
                        DestroyCollider(orb);
                        orb.transform.SetParent(deco, false);
                        float ox = (float)(rng.NextDouble() - 0.5) * cellSize * 0.4f;
                        float oz = (float)(rng.NextDouble() - 0.5) * cellSize * 0.4f;
                        orb.transform.localPosition = CellCenter(x, z) + new Vector3(ox, 1.3f + (float)rng.NextDouble() * 0.8f, oz);
                        orb.transform.localScale = Vector3.one * (0.25f + (float)rng.NextDouble() * 0.2f);
                        orb.GetComponent<MeshRenderer>().sharedMaterial = orbMaterials[rng.Next(orbMaterials.Length)];
                    }
            }

            // Objectif : au point le plus éloigné du départ
            var trophy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trophy.name = "Objectif";
            trophy.transform.SetParent(deco, false);
            trophy.transform.localPosition = CellCenter(goal.x, goal.y) + Vector3.up * 0.5f;
            trophy.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            if (goalMaterial != null) trophy.GetComponent<MeshRenderer>().sharedMaterial = goalMaterial;
            var gl = new GameObject("Lumiere Objectif").AddComponent<Light>();
            gl.transform.SetParent(trophy.transform, false);
            gl.transform.localPosition = Vector3.up * 2f;
            gl.type = LightType.Point;
            gl.color = new Color(1f, 0.8f, 0.3f);
            gl.range = 5f;
            gl.intensity = 3f;
            gl.shadows = LightShadows.None;
        }

        void BuildReflectionProbe(Transform root)
        {
            // Probe globale : filet de sécurité, priorité basse.
            var go = new GameObject("Reflection Probe");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(width * cellSize * 0.5f, 1.6f, depth * cellSize * 0.5f);
            var probe = go.AddComponent<ReflectionProbe>();
            if (fallbackReflection != null)
            {
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                probe.customBakedTexture = fallbackReflection;
            }
            else probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.size = new Vector3(width * cellSize + 2f, wallHeight + 1f, depth * cellSize + 2f);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.importance = 0;

            // Une probe par case : le reflet de secours (miroirs non rendus ou trop profonds)
            // montre alors le bon couloir, flou, au lieu d'un mur sombre.
            if (!perCellProbes) return;
            var parent = new GameObject("Reflets de secours (par case)").transform;
            parent.SetParent(root, false);
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                {
                    var pgo = new GameObject($"Case_{x}_{z}");
                    pgo.transform.SetParent(parent, false);
                    pgo.transform.localPosition = CellCenter(x, z) + Vector3.up * 1.6f;
                    var p = pgo.AddComponent<ReflectionProbe>();
                    p.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                    p.size = new Vector3(cellSize, wallHeight + 0.4f, cellSize);
                    p.center = new Vector3(0f, wallHeight * 0.5f - 1.6f, 0f);
                    p.boxProjection = true;
                    p.resolution = probeResolution;
                    p.importance = 1;
                    p.blendDistance = 0.2f;
#if UNITY_EDITOR
                    p.customBakedTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(ProbePath(pgo.name));
#endif
                }
        }

#if UNITY_EDITOR
        const string ProbeFolder = "Assets/PalaisDesGlaces/Textures/Probes";
        static string ProbePath(string probeName) => $"{ProbeFolder}/{probeName.Replace(' ', '_')}.exr";

        /// <summary>
        /// Calcule les reflets de secours (toutes les reflection probes du labyrinthe).
        /// 2 passes : à la 2e, les miroirs vus par les probes contiennent déjà un reflet.
        /// </summary>
        [ContextMenu("Baker les reflets de secours")]
        public void BakeFallbackReflections() => BakeFallbackReflections(2);

        public void BakeFallbackReflections(int passes)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(ProbeFolder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/PalaisDesGlaces/Textures", "Probes");
            var probes = GetComponentsInChildren<ReflectionProbe>();
            for (int pass = 0; pass < passes; pass++)
                foreach (var p in probes)
                {
                    string path = ProbePath(p.gameObject.name);
                    UnityEditor.Lightmapping.BakeReflectionProbe(p, path);
                    p.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                    p.customBakedTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if (p.importance == 0) fallbackReflection = p.customBakedTexture;
                }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        static GameObject CreateCube(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c == null) return;
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }
}
