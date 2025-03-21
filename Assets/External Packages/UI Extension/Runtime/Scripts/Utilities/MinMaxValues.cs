///Credit brogan89
///Sourced from - https://github.com/brogan89/MinMaxSlider

using System;

namespace UnityEngine.UI.Extensions
{
    [Serializable]
    public struct MinMaxValues
    {
        /// <summary>
        ///     Floating point tolerance
        /// </summary>
        public const float FLOAT_TOL = 0.01f;

        public static MinMaxValues DEFUALT = new(25, 75, 0, 100);

        public float minValue, maxValue, minLimit, maxLimit;

        public MinMaxValues(float minValue, float maxValue, float minLimit, float maxLimit)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.minLimit = minLimit;
            this.maxLimit = maxLimit;
        }

        /// <summary>
        ///     Constructor for when values equal limits
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        public MinMaxValues(float minValue, float maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            minLimit = minValue;
            maxLimit = maxValue;
        }

        public bool IsAtMinAndMax() =>
            Math.Abs(minValue - minLimit) < FLOAT_TOL && Math.Abs(maxValue - maxLimit) < FLOAT_TOL;

        public override string ToString() =>
            $"Values(min:{minValue}, max:{maxValue}) | Limits(min:{minLimit}, max:{maxLimit})";
    }
}