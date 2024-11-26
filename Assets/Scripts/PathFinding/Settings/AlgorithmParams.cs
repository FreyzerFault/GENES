using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GENES.PathFinding.Settings
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
        
        public ParamValue(float value, string displayName)
        {
            this.value = value;
            this.displayName = displayName;
        }
    }
    
    // SerializedDictionary no se puede Serializar si no creo una clase heredada simple sin generics
    [Serializable]
    public sealed class ParamDictionary : SerializedDictionary<ParamType, ParamValue>
    {
        public Dictionary<ParamType, ParamValue> ToDictionary()
        {
            return this;
        }
    }
    
    [CreateAssetMenu(fileName = "Algorithm Parameters", menuName = "PathFinding/Algorithm Parameters")]
    public abstract class AlgorithmParams : ScriptableObject
    {
        protected abstract float[] DefaultValue { get; }
        public abstract string[] DisplayNames { get; }
        public abstract ParamType[] Types { get; }

        [SerializeField] public ParamDictionary parameters;

        private void Awake() => SetDefaultValues();
        private void Reset() => SetDefaultValues();

        protected void SetDefaultValues()
        {
            var values = DisplayNames
                .Select((displayName, i) => new ParamValue(i < DefaultValue.Length ? DefaultValue[i] : 1f, displayName))
                .ToArray();
            parameters = new ParamDictionary();
            for (var i = 0; i < Types.Length; i++)
                parameters[Types[i]] = values[i];
        }

        protected void SetParameters(IDictionary<ParamType, ParamValue> p)
        {
            parameters = new ParamDictionary();
			
            foreach (KeyValuePair<ParamType,ParamValue> kvp in p) 
                parameters[kvp.Key] = kvp.Value;
        }
        
        public float GetValue(ParamType param) => parameters[param].value;
        public string GetDisplayName(ParamType param) => parameters[param].displayName;

        public void SetValue(ParamType param, float value) =>
            parameters[param] = new ParamValue { value = value, displayName = GetDisplayName(param) };
    }
}
