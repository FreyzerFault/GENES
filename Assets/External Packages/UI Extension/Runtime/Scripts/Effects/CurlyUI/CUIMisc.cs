/// Credit Titinious (https://github.com/Titinious)
/// Sourced from - https://github.com/Titinious/CurlyUI

using System;

namespace UnityEngine.UI.Extensions
{
    [Serializable]
    public struct Vector3_Array2D
    {
        [SerializeField] public Vector3[] array;

        public Vector3 this[int _idx]
        {
            get => array[_idx];
            set => array[_idx] = value;
        }
    }
}