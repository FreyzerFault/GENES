using System;
using UnityEngine;

namespace GENES.TreesGeneration
{
    [Serializable]
    public class ForestGenSettings: TreesGenSettings
    {
        [Range(0.01f, 20)]public float minSeparation;
        [Range(0.01f, 1)]public float density;

        public ForestGenSettings() {}

        public ForestGenSettings(float maxSlopeAngle)
        {
            this.maxSlopeAngle = maxSlopeAngle;
        }
    }
}
