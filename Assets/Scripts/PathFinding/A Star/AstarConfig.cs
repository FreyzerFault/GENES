using System;
using System.Linq;
using Utils;

namespace PathFinding.A_Star
{
    [Serializable]
    public enum AstarParam
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


    [Serializable]
    public class AstarConfig
    {
        private const float DefaultValue = 1f;
        private static string[] _displayNames = { "Distance", "Height", "Turn", "Distance", "Height", "Slope" };

        private static AstarParam[] _paramsKeys =
        {
            AstarParam.DistanceCost,
            AstarParam.HeightCost,
            AstarParam.TurnCost,
            AstarParam.DistanceHeuristic,
            AstarParam.HeightHeuristic,
            AstarParam.SlopeHeuristic
        };

        public DictionarySerializable<AstarParam, ParamValue> parameters;

        public AstarConfig(DictionarySerializable<AstarParam, ParamValue> parameters = null)
        {
            var values = _displayNames.Select(name => new ParamValue { value = DefaultValue, displayName = name })
                .ToArray();
            this.parameters = parameters ??
                              new DictionarySerializable<AstarParam, ParamValue>(_paramsKeys, values);
        }

        // Escalado de Coste y Heurística
        // Ajusta la penalización o recompensa de cada parámetro

        // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

        // =================== Coste ===================
        // Penaliza la Distancia Recorrida (Ruta + corta)
        public float DistanceCost
        {
            get => GetValue(AstarParam.DistanceCost);
            set => SetValue(AstarParam.DistanceCost, value);
        }

        // Penaliza cambiar la altura (evita que intente subir escalones grandes)
        public float HeightCost
        {
            get => GetValue(AstarParam.HeightCost);
            set => SetValue(AstarParam.HeightCost, value);
        }

        // Penaliza giros bruscos 
        public float TurnCost
        {
            get => GetValue(AstarParam.TurnCost);
            set => SetValue(AstarParam.TurnCost, value);
        }

        // =================== Heurística ===================
        // Recompensa acercarse al objetivo
        public float DistanceHeuristic
        {
            get => GetValue(AstarParam.DistanceHeuristic);
            set => SetValue(AstarParam.DistanceHeuristic, value);
        }

        // Recompensa acercarse a la altura del objetivo
        public float HeightHeuristic
        {
            get => GetValue(AstarParam.HeightHeuristic);
            set => SetValue(AstarParam.HeightHeuristic, value);
        }

        // Recompensa minimizar la pendiente (rodea montículos si puede)
        public float SlopeHeuristic
        {
            get => GetValue(AstarParam.SlopeHeuristic);
            set => SetValue(AstarParam.SlopeHeuristic, value);
        }

        // ======================== MANIPULACIÓN DE PARÁMETROS ========================

        // Value
        public float GetValue(AstarParam param) => parameters.GetValue(param).value;

        // Display Names
        public string GetDisplayName(AstarParam param) => parameters.GetValue(param).displayName;

        // Setter
        public void SetValue(AstarParam param, float value) =>
            parameters.SetValue(param, new ParamValue { value = value, displayName = GetDisplayName(param) });
    }
}