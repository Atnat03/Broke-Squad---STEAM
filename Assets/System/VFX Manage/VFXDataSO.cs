using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VFX_Manage
{
    [CreateAssetMenu(fileName = "VFX", menuName = "VFX/newVFX")]
    public class VFXDataSO  :ScriptableObject
    {
        public List<VfxData> vfxDataList = new List<VfxData>();
        public string GetKey(VfxData v) => $"{name}/{v.vfxName}";
    }

    [Serializable]
    public class VfxData
    {
        public string vfxName;
        public GameObject vfxPrefab;
    }
}
