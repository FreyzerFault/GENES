using System;
using UnityEngine;

namespace GENES.TreesGeneration
{
    [Serializable]
    public abstract class TreesGenSettings
    {
        [Range(1, 90)] public float maxSlopeAngle = 30;
    }
}
