using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PalaisDesGlaces
{
    /// <summary>
    /// À mettre sur la caméra du joueur. Rend les reflets des miroirs de façon récursive
    /// (reflet du reflet du reflet...) avec des limites dures pour ne jamais exploser :
    ///  - profondeur maximale (recursionDepth),
    ///  - nombre de miroirs suivis dans chaque reflet (childrenPerMirror),
    ///  - nombre TOTAL de rendus par frame (maxRendersPerFrame).
    ///
    /// Chaque frame :
    ///  1. on construit un arbre de reflets niveau par niveau. À chaque niveau, le budget restant
    ///     va aux reflets les plus grands à l'écran, avec un bonus pour ceux déjà affichés à la
    ///     frame précédente (stabilité : pas de clignotement quand deux miroirs se valent) ;
    ///  2. on rend l'arbre du plus profond vers la caméra.
    /// Chaque reflet apparaît en fondu. Au-delà de la profondeur max, les miroirs affichent un
    /// reflet de secours (reflection probe) de plus en plus sombre.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(1000)]
    public class MirrorReflectionManager : MonoBehaviour
    {
        [Header("Profondeur des reflets")]
        [Tooltip("Nombre de reflets imbriqués (1 = simple miroir, 3 = reflet du reflet du reflet).")]
        [Range(1, 5)] public int recursionDepth = 3;
        [Tooltip("Nombre max de miroirs suivis à l'intérieur de chaque reflet.")]
        [Range(1, 4)] public int childrenPerMirror = 3;

        [Header("Budget (anti-crash)")]
        [Tooltip("Nombre max de miroirs vus directement qui reçoivent un vrai reflet.")]
        [Range(1, 16)] public int maxFirstLevelMirrors = 8;
        [Tooltip("Nombre TOTAL de rendus de reflets par frame, toutes profondeurs confondues. Limite dure.")]
        [Range(1, 64)] public int maxRendersPerFrame = 24;
        [Tooltip("Résolution des reflets par profondeur (1er reflet, 2e, 3e...), relative à l'écran.")]
        public float[] resolutionPerDepth = { 0.6f, 0.4f, 0.3f, 0.25f, 0.2f };
        [Tooltip("Distance parcourue par la lumière (rebonds compris) au-delà de laquelle on arrête.")]
        public float maxDistance = 40f;
        [Tooltip("Ignore les miroirs qui occupent moins que cette fraction de l'écran.")]
        public float minScreenArea = 0.0005f;

        [Header("Stabilité / transitions")]
        [Tooltip("Durée du fondu quand un reflet apparaît (à tous les niveaux).")]
        public float fadeInTime = 0.25f;
        [Tooltip("Bonus de priorité pour les reflets déjà affichés à la frame précédente. Plus haut = plus stable.")]
        public float stickyBonus = 2.5f;
        [Tooltip("Assombrissement du reflet de secours à chaque niveau de profondeur.")]
        [Range(0f, 1f)] public float fallbackDarkenPerDepth = 0.08f;

        [Header("Rendu des reflets")]
        public LayerMask reflectionCullingMask = ~0;
        public bool reflectionShadows = false;
        public float clipPlaneOffset = 0.01f;
        [Tooltip("Plan lointain des caméras de reflet. Grand = moins de découpe quand on regarde un miroir en rasant.")]
        public float obliqueFarPlane = 2000f;

        [Header("Occlusion")]
        [Tooltip("Ignore les miroirs cachés derrière des murs (raycasts).")]
        public bool occlusionCheck = true;
        public LayerMask occluderMask = ~0;

        [Header("Debug")]
        public bool showStats = true;

        public int RendersLastFrame { get; private set; }
        /// <summary>Nombre de reflets qui sont apparus cette frame (n'étaient pas là à la précédente).</summary>
        public int AppearedLastFrame { get; private set; }

        struct View
        {
            public Matrix4x4 worldToCam;
            public Vector3 pos;
            public Rect rect;            // zone de l'écran où cette vue est visible
            public PlanarMirror through; // miroir à travers lequel on regarde (null = caméra réelle)
        }

        struct Candidate
        {
            public PlanarMirror mirror;
            public float score;
        }

        struct Node
        {
            public PlanarMirror mirror;
            public View view;     // caméra virtuelle de ce reflet
            public int parent;    // -1 = miroir vu directement
            public int depth;
            public long key;      // identifie le chemin (miroir -> sous-miroir...) d'une frame à l'autre
            public float fade;
            public RenderTexture rt;
        }

        struct Pending
        {
            public int parent;
            public Candidate candidate;
        }

        static readonly int DepthDarkenId = Shader.PropertyToID("_MirrorDepthDarken");
        static readonly System.Comparison<Candidate> ByScore = (a, b) => b.score.CompareTo(a.score);
        static readonly System.Comparison<Pending> ByPendingScore = (a, b) => b.candidate.score.CompareTo(a.candidate.score);

        Camera _cam;
        Camera _reflCam;
        Matrix4x4 _proj;
        int _maxDepthReached;
        float _smoothDt = 0.016f;
        float _appearRate;

        readonly List<Node> _nodes = new List<Node>(64);
        readonly List<Pending> _pending = new List<Pending>(64);
        readonly List<Candidate> _candidates = new List<Candidate>(16);
        Dictionary<long, float> _fade = new Dictionary<long, float>();
        Dictionary<long, float> _fadeNext = new Dictionary<long, float>();
        HashSet<long> _prevKeys = new HashSet<long>();
        HashSet<long> _curKeys = new HashSet<long>();

        readonly List<List<RenderTexture>> _pools = new List<List<RenderTexture>>();
        readonly List<int> _poolIndex = new List<int>();
        readonly List<PlanarMirror> _assigned = new List<PlanarMirror>();
        readonly List<PlanarMirror> _childMirrors = new List<PlanarMirror>();
        readonly List<RenderTexture> _childRts = new List<RenderTexture>();
        readonly List<float> _childBlends = new List<float>();
        readonly Plane[] _frustum = new Plane[6];

        void Awake()
        {
            _cam = GetComponent<Camera>();
            Shader.SetGlobalFloat(DepthDarkenId, 0f);
        }

        void OnDisable()
        {
            foreach (var m in PlanarMirror.All) m.SetReflection(null);
            foreach (var pool in _pools)
                foreach (var rt in pool) ReleaseRT(rt);
            _pools.Clear();
            _poolIndex.Clear();
            _fade.Clear();
            _prevKeys.Clear();
            if (_reflCam != null) Destroy(_reflCam.gameObject);
            _reflCam = null;
        }

        // ------------------------------------------------------------------ frame

        void LateUpdate()
        {
            if (_cam == null) return;
            EnsureReflectionCamera();
            BeginFrame();

            var root = new View
            {
                worldToCam = _cam.worldToCameraMatrix,
                pos = _cam.transform.position,
                rect = new Rect(0f, 0f, 1f, 1f),
                through = null
            };

            BuildTree(root);
            UpdateFades();
            RenderTree();
            ApplyToMainCamera();

            // On garde la mémoire de cette frame pour la stabilité de la suivante
            (_prevKeys, _curKeys) = (_curKeys, _prevKeys);
            _curKeys.Clear();
            (_fade, _fadeNext) = (_fadeNext, _fade);
            _fadeNext.Clear();
        }

        void BeginFrame()
        {
            for (int i = 0; i < _poolIndex.Count; i++) _poolIndex[i] = 0;
            RendersLastFrame = 0;
            _maxDepthReached = 0;
            _proj = _cam.projectionMatrix;

            // Aucun miroir ne doit garder la texture de la frame précédente pendant les rendus.
            foreach (var m in PlanarMirror.All)
                if (m.HasReflection) m.SetReflection(null);
            _assigned.Clear();

            _reflCam.aspect = _cam.aspect;
            _reflCam.fieldOfView = _cam.fieldOfView;
            _reflCam.nearClipPlane = _cam.nearClipPlane;
            _reflCam.farClipPlane = Mathf.Max(_cam.farClipPlane, obliqueFarPlane);
            _reflCam.clearFlags = _cam.clearFlags;
            _reflCam.backgroundColor = _cam.backgroundColor;
            _reflCam.cullingMask = reflectionCullingMask;
        }

        // ------------------------------------------------------------------ arbre des reflets

        void BuildTree(View root)
        {
            _nodes.Clear();
            int budget = maxRendersPerFrame;

            // Niveau 1 : miroirs vus directement
            _candidates.Clear();
            SelectChildren(root, Mathf.Min(maxFirstLevelMirrors, budget), _candidates, 0L);
            for (int i = 0; i < _candidates.Count; i++) AddNode(root, _candidates[i].mirror, -1, 1, 0L);
            budget -= _candidates.Count;

            // Niveaux suivants : le budget va aux reflets les plus importants de tout le niveau
            int levelStart = 0, levelEnd = _nodes.Count;
            for (int depth = 2; depth <= recursionDepth && budget > 0 && levelEnd > levelStart; depth++)
            {
                _pending.Clear();
                for (int i = levelStart; i < levelEnd; i++)
                {
                    _candidates.Clear();
                    SelectChildren(_nodes[i].view, childrenPerMirror, _candidates, _nodes[i].key);
                    for (int c = 0; c < _candidates.Count; c++)
                        _pending.Add(new Pending { parent = i, candidate = _candidates[c] });
                }

                _pending.Sort(ByPendingScore);
                int take = Mathf.Min(budget, _pending.Count);
                for (int k = 0; k < take; k++)
                {
                    var parent = _nodes[_pending[k].parent];
                    AddNode(parent.view, _pending[k].candidate.mirror, _pending[k].parent, depth, parent.key);
                }
                budget -= take;
                levelStart = levelEnd;
                levelEnd = _nodes.Count;
            }
        }

        void AddNode(View viewer, PlanarMirror mirror, int parent, int depth, long parentKey)
        {
            Matrix4x4 reflection = ReflectionMatrix(mirror.Normal, mirror.Position);
            _nodes.Add(new Node
            {
                mirror = mirror,
                view = new View
                {
                    worldToCam = viewer.worldToCam * reflection,
                    pos = reflection.MultiplyPoint(viewer.pos),
                    rect = Intersect(viewer.rect, ScreenRect(viewer.worldToCam, mirror)),
                    through = mirror
                },
                parent = parent,
                depth = depth,
                key = PathKey(parentKey, mirror)
            });
        }

        static long PathKey(long parentKey, PlanarMirror mirror) => unchecked(parentKey * 1000003L + mirror.GetInstanceID());

        void UpdateFades()
        {
            float step = fadeInTime > 0f ? Time.unscaledDeltaTime / fadeInTime : 1f;
            int appeared = 0;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (!_prevKeys.Contains(node.key)) appeared++;
                _fade.TryGetValue(node.key, out float f);
                node.fade = Mathf.MoveTowards(f, 1f, step);
                _fadeNext[node.key] = node.fade;
                _curKeys.Add(node.key);
                _nodes[i] = node;
            }
            AppearedLastFrame = appeared;
        }

        void RenderTree()
        {
            bool oldCulling = GL.invertCulling;
            try
            {
                // Les enfants ont toujours un index plus grand que leur parent :
                // en parcourant à l'envers, chaque reflet est rendu après ceux qu'il contient.
                for (int i = _nodes.Count - 1; i >= 0; i--)
                {
                    var node = _nodes[i];
                    CollectChildren(i);
                    Assign(_childMirrors, _childRts, _childBlends);
                    node.rt = GetRT(node.depth);
                    RenderView(node.view, node.mirror, node.depth, node.rt);
                    _nodes[i] = node;
                }
            }
            finally
            {
                GL.invertCulling = oldCulling;
                Shader.SetGlobalFloat(DepthDarkenId, 0f);
            }
        }

        void ApplyToMainCamera()
        {
            CollectChildren(-1);
            Assign(_childMirrors, _childRts, _childBlends);
            _assigned.Clear();
        }

        void CollectChildren(int parent)
        {
            _childMirrors.Clear();
            _childRts.Clear();
            _childBlends.Clear();
            for (int j = parent + 1; j < _nodes.Count; j++)
            {
                if (_nodes[j].parent != parent) continue;
                _childMirrors.Add(_nodes[j].mirror);
                _childRts.Add(_nodes[j].rt);
                _childBlends.Add(_nodes[j].fade);
            }
        }

        void Assign(List<PlanarMirror> mirrors, List<RenderTexture> rts, List<float> blends)
        {
            for (int i = 0; i < _assigned.Count; i++)
                if (!mirrors.Contains(_assigned[i])) _assigned[i].SetReflection(null);
            _assigned.Clear();
            for (int i = 0; i < mirrors.Count; i++)
            {
                mirrors[i].SetReflection(rts[i], blends[i]);
                _assigned.Add(mirrors[i]);
            }
        }

        void RenderView(View view, PlanarMirror mirror, int depth, RenderTexture target)
        {
            Vector3 n = mirror.Normal;
            Vector3 p = mirror.Position;

            Matrix4x4 camToWorld = view.worldToCam.inverse;
            _reflCam.transform.SetPositionAndRotation(view.pos,
                Quaternion.LookRotation(camToWorld.MultiplyVector(Vector3.back), camToWorld.MultiplyVector(Vector3.up)));
            _reflCam.worldToCameraMatrix = view.worldToCam;
            // Plan de clipping oblique : rien de ce qui est derrière le miroir n'est rendu.
            // Le plan lointain est repoussé avant le calcul oblique pour limiter la découpe
            // quand on regarde un miroir en rasant.
            _reflCam.ResetProjectionMatrix();
            _reflCam.projectionMatrix = _reflCam.CalculateObliqueMatrix(CameraSpacePlane(view.worldToCam, p, n));
            _reflCam.targetTexture = target;

            GL.invertCulling = (depth & 1) == 1; // chaque réflexion inverse l'ordre des faces
            Shader.SetGlobalFloat(DepthDarkenId, Mathf.Clamp01(fallbackDarkenPerDepth * depth));
            _reflCam.Render();
            _reflCam.targetTexture = null;

            RendersLastFrame++;
            if (depth > _maxDepthReached) _maxDepthReached = depth;
        }

        // ------------------------------------------------------------------ sélection

        void SelectChildren(View view, int max, List<Candidate> output, long parentKey)
        {
            if (max <= 0) return;
            GeometryUtility.CalculateFrustumPlanes(_proj * view.worldToCam, _frustum);
            float maxSqr = maxDistance * maxDistance;

            foreach (var m in PlanarMirror.All)
            {
                if (m == view.through || m.Renderer == null || !m.Renderer.enabled) continue;
                Vector3 n = m.Normal, p = m.Position;
                if (Vector3.Dot(view.pos - p, n) <= 0.01f) continue; // vu de dos

                Bounds b = m.Renderer.bounds;
                if (view.through != null && !InFrontOf(b, view.through)) continue; // entièrement derrière le miroir parent
                if (b.SqrDistance(view.pos) > maxSqr) continue;
                if (!GeometryUtility.TestPlanesAABB(_frustum, b)) continue;

                Rect r = Intersect(view.rect, ScreenRect(view.worldToCam, m));
                float area = r.width * r.height;
                if (area < minScreenArea) continue;
                if (occlusionCheck && !IsVisible(view, m)) continue;

                float score = area;
                if (_prevKeys.Contains(PathKey(parentKey, m))) score *= stickyBonus;
                output.Add(new Candidate { mirror = m, score = score });
            }

            output.Sort(ByScore);
            if (output.Count > max) output.RemoveRange(max, output.Count - max);
        }

        /// <summary>Vrai si une partie de la boîte est devant le plan du miroir (les longs murs peuvent avoir leur centre derrière).</summary>
        static bool InFrontOf(Bounds b, PlanarMirror plane)
        {
            Vector3 n = plane.Normal;
            float centerDist = Vector3.Dot(b.center - plane.Position, n);
            float radius = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z)));
            return centerDist + radius > 0.01f;
        }

        bool IsVisible(View view, PlanarMirror m)
        {
            Transform t = m.transform;
            int nx = Mathf.Clamp(Mathf.CeilToInt(t.lossyScale.x / 1.5f) + 1, 3, 12);
            Vector3 off = m.Normal * 0.05f;
            for (int iy = -1; iy <= 1; iy++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float lx = Mathf.Lerp(-0.45f, 0.45f, ix / (float)(nx - 1));
                    Vector3 s = t.TransformPoint(lx, iy * 0.4f, 0f) + off;
                    if (SampleVisible(view, s)) return true;
                }
            return false;
        }

        bool SampleVisible(View view, Vector3 sample)
        {
            Vector3 from = view.pos;
            if (view.through != null)
            {
                // Le rayon virtuel traverse le miroir parent : le vrai trajet de la lumière
                // part du point d'impact sur ce miroir.
                var parent = view.through;
                Vector3 pn = parent.Normal;
                Vector3 dir = sample - view.pos;
                float denom = Vector3.Dot(pn, dir);
                if (Mathf.Abs(denom) < 1e-5f) return false;
                float tHit = Vector3.Dot(pn, parent.Position - view.pos) / denom;
                if (tHit <= 0f || tHit >= 1f) return false;
                Vector3 hit = view.pos + dir * tHit;
                Vector3 local = parent.transform.InverseTransformPoint(hit);
                if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.y) > 0.5f) return false;
                from = hit + pn * 0.05f;
            }
            return !Physics.Linecast(from, sample, occluderMask, QueryTriggerInteraction.Ignore);
        }

        Rect ScreenRect(Matrix4x4 worldToCam, PlanarMirror m)
        {
            Matrix4x4 vp = _proj * worldToCam;
            Transform t = m.transform;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            int behind = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 w = t.TransformPoint((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, 0f);
                Vector4 c = vp * new Vector4(w.x, w.y, w.z, 1f);
                if (c.w <= 0.0001f) { behind++; continue; }
                float x = c.x / c.w * 0.5f + 0.5f, y = c.y / c.w * 0.5f + 0.5f;
                xMin = Mathf.Min(xMin, x); xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
            }
            if (behind == 4) return Rect.zero;
            if (behind > 0) return new Rect(0f, 0f, 1f, 1f); // en partie derrière la caméra : prudence
            return Rect.MinMaxRect(Mathf.Clamp01(xMin), Mathf.Clamp01(yMin), Mathf.Clamp01(xMax), Mathf.Clamp01(yMax));
        }

        static Rect Intersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin), yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax), yMax = Mathf.Min(a.yMax, b.yMax);
            return xMax <= xMin || yMax <= yMin ? Rect.zero : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // ------------------------------------------------------------------ maths

        Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 pos, Vector3 normal)
        {
            Vector3 cpos = worldToCamera.MultiplyPoint(pos + normal * clipPlaneOffset);
            Vector3 cnormal = worldToCamera.MultiplyVector(normal).normalized;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        static Matrix4x4 ReflectionMatrix(Vector3 n, Vector3 p)
        {
            float d = -Vector3.Dot(n, p);
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = 1f - 2f * n.x * n.x; m.m01 = -2f * n.x * n.y;      m.m02 = -2f * n.x * n.z;      m.m03 = -2f * d * n.x;
            m.m10 = -2f * n.y * n.x;      m.m11 = 1f - 2f * n.y * n.y; m.m12 = -2f * n.y * n.z;      m.m13 = -2f * d * n.y;
            m.m20 = -2f * n.z * n.x;      m.m21 = -2f * n.z * n.y;      m.m22 = 1f - 2f * n.z * n.z; m.m23 = -2f * d * n.z;
            return m;
        }

        // ------------------------------------------------------------------ ressources

        void EnsureReflectionCamera()
        {
            if (_reflCam != null) return;
            var go = new GameObject("Mirror Reflection Camera") { hideFlags = HideFlags.HideAndDontSave };
            _reflCam = go.AddComponent<Camera>();
            _reflCam.enabled = false; // rendu uniquement à la main
            var data = _reflCam.GetUniversalAdditionalCameraData();
            data.renderShadows = reflectionShadows;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        RenderTexture GetRT(int depth)
        {
            int level = depth - 1;
            while (_pools.Count <= level) { _pools.Add(new List<RenderTexture>()); _poolIndex.Add(0); }

            float scale = resolutionPerDepth != null && resolutionPerDepth.Length > 0
                ? resolutionPerDepth[Mathf.Min(level, resolutionPerDepth.Length - 1)]
                : 0.5f;
            int w = Mathf.Max(32, Mathf.RoundToInt(_cam.pixelWidth * scale));
            int h = Mathf.Max(32, Mathf.RoundToInt(_cam.pixelHeight * scale));

            var pool = _pools[level];
            int idx = _poolIndex[level]++;
            if (idx < pool.Count && (pool[idx].width != w || pool[idx].height != h))
            {
                ReleaseRT(pool[idx]);
                pool[idx] = CreateRT(w, h, level, idx);
            }
            else if (idx >= pool.Count)
            {
                pool.Add(CreateRT(w, h, level, idx));
            }
            return pool[idx];
        }

        static RenderTexture CreateRT(int w, int h, int level, int idx)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR)
            {
                name = $"MirrorReflection_D{level + 1}_{idx}",
                hideFlags = HideFlags.DontSave,
                useMipMap = false,
                antiAliasing = 1
            };
            rt.Create();
            return rt;
        }

        static void ReleaseRT(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Destroy(rt);
        }

        void OnGUI()
        {
            if (!showStats || Event.current.type != EventType.Repaint) return;
            _smoothDt = Mathf.Lerp(_smoothDt, Time.unscaledDeltaTime, 0.05f);
            _appearRate = Mathf.Lerp(_appearRate, AppearedLastFrame / Mathf.Max(Time.unscaledDeltaTime, 0.001f), 0.05f);
            GUI.Label(new Rect(10, 10, 600, 22),
                $"{1f / _smoothDt:0} FPS  |  reflets : {RendersLastFrame}/{maxRendersPerFrame}  |  profondeur : {_maxDepthReached}  |  apparitions/s : {_appearRate:0}");
        }
    }
}
