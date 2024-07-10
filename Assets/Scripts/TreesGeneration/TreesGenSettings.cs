using System;
using UnityEngine;

namespace TreesGeneration
{
    [Serializable]
    public abstract class TreesGenSettings
    {
        [Range(1, 90)] public float maxSlopeAngle;
    }
}
