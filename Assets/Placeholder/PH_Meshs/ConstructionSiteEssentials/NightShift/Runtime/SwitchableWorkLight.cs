using UnityEngine;

namespace ConstructionSiteEssentials.NightShift
{
    public sealed class SwitchableWorkLight : MonoBehaviour
    {
        public Light[] lamps = new Light[0];
        public Renderer[] ledFaces = new Renderer[0];
        public Material poweredFace;
        public bool startsOn;
        public bool IsOn { get; private set; }
        Material[] unlitFaces;

        void Awake()
        {
            unlitFaces = new Material[ledFaces.Length];
            for (int i = 0; i < ledFaces.Length; i++)
                if (ledFaces[i]) unlitFaces[i] = ledFaces[i].sharedMaterial;
            SetPower(startsOn);
        }

        public void Toggle() { SetPower(!IsOn); }

        public void SetPower(bool on)
        {
            IsOn = on;
            foreach (Light lamp in lamps)
                if (lamp) lamp.enabled = on;
            if (unlitFaces == null || !poweredFace) return;
            for (int i = 0; i < ledFaces.Length; i++)
                if (ledFaces[i]) ledFaces[i].sharedMaterial = on ? poweredFace : unlitFaces[i];
        }
    }
}
