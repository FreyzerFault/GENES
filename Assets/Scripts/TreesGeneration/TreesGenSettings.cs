using System;
using DavidUtils.DevTools.CustomAttributes;
using UnityEngine;

namespace GENES.TreesGeneration
{
    [Serializable]
    public abstract class TreesGenSettings: ArrayElementTitleAttribute.IArrayElementTitle
    {
        public virtual RegionType RegionType => RegionType.Olive;
        [Range(1, 90)] public float maxSlopeAngle = 30;
        public virtual string Name => RegionType.ToString();
    }
}
