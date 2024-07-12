using System;
using DavidUtils.Collections;
using UnityEngine;

namespace PathFinding.Settings
{
    [Serializable]
    public enum ParamType
    {
        DistanceCost,
        HeightCost,
        TurnCost,
        DistanceHeuristic,
        HeightHeuristic,
        SlopeHeuristic
    }
    
    [Serializable]
    public struct ParamValue
    {
        public float value;
        public string displayName;
    }
    
    [CreateAssetMenu(fileName = "Algorithm Parameters", menuName = "PathFinding/Algorithm Parameters")]
    public abstract class AlgorithmParams : ScriptableObject
    {
        private const float DefaultValue = 1f;

        public DictionarySerializable<ParamType, ParamValue> parameters;

        public float GetValue(ParamType param) => parameters.GetValue(param).value;
        public string GetDisplayName(ParamType param) => parameters.GetValue(param).displayName;

        public void SetValue(ParamType param, float value) =>
            parameters.SetValue(param, new ParamValue { value = value, displayName = GetDisplayName(param) });
    }
}
