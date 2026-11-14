using System.Collections.Generic;
using UnityEngine;

namespace PalaisDesGlaces
{
    /// <summary>
    /// Surface de miroir (un Quad avec le matériau PlanarMirror).
    /// Ne fait aucun rendu elle-même : c'est MirrorReflectionManager qui décide
    /// quels miroirs reçoivent un vrai reflet, et à quelle profondeur.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PlanarMirror : MonoBehaviour
    {
        public static readonly List<PlanarMirror> All = new List<PlanarMirror>();

        static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
        static readonly int ReflectionBlendId = Shader.PropertyToID("_ReflectionBlend");

        public Renderer Renderer { get; private set; }

        MaterialPropertyBlock _mpb;
        Texture _current;
        float _blend = -1f;

        /// <summary>Normale de la face visible (un Quad Unity regarde vers -Z local).</summary>
        public Vector3 Normal => -transform.forward;
        public Vector3 Position => transform.position;
        public bool HasReflection => _current != null;

        void OnEnable()
        {
            Renderer = GetComponent<Renderer>();
            _mpb ??= new MaterialPropertyBlock();
            All.Add(this);
            _blend = -1f;
            SetReflection(null);
        }

        void OnDisable()
        {
            All.Remove(this);
            SetReflection(null);
        }

        /// <summary>tex = null : reflet de secours uniquement. blend : 0 = secours, 1 = vrai reflet.</summary>
        public void SetReflection(Texture tex, float blend = 1f)
        {
            if (tex == null) blend = 0f;
            if (tex == _current && Mathf.Approximately(blend, _blend)) return;
            _current = tex;
            _blend = blend;

            Renderer.GetPropertyBlock(_mpb);
            if (tex != null) _mpb.SetTexture(ReflectionTexId, tex);
            _mpb.SetFloat(ReflectionBlendId, blend);
            Renderer.SetPropertyBlock(_mpb);
        }
    }
}
