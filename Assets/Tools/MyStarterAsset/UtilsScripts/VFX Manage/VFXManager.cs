using Bus;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VFX_Manage
{
    public class VFXManager : MonoBusListener
    {
        [SerializeField] private List<VFXDataSO> vfxData = new();
        [SerializeField] private int maxPerVfx = 20;

        private readonly Dictionary<string, GameObject> prefabs = new();
        private readonly Dictionary<string, Queue<PoolVfx>> free = new();
        private readonly Dictionary<string, int> created = new();
        private bool vfxEnabled = true;
    }
}
