using UnityEngine;

namespace TreesGeneration
{
    public class ForestGenSettings: TreesGenSettings
    {
        [Range(0.01f, 20)]public float minSeparation;
        [Range(0.01f, 1)]public float density;
    }
}
