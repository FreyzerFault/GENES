using System;
using Utils;
using KeyValuePairS = Utils.DictionarySerializable<PathFinding.A_Star.AstarParam, float>.KeyValuePairSerializable;

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
    public struct AstarConfig
    {
        private const float DefaultValue = 1f;
        public DictionarySerializable<AstarParam, float> parameters;

        public AstarConfig(DictionarySerializable<AstarParam, float> parameters = null) =>
            this.parameters = parameters ?? new DictionarySerializable<AstarParam, float>(new[]
            {
                new KeyValuePairS { key = AstarParam.DistanceCost, value = DefaultValue },
                new KeyValuePairS { key = AstarParam.HeightCost, value = DefaultValue },
                new KeyValuePairS { key = AstarParam.TurnCost, value = DefaultValue },
                new KeyValuePairS { key = AstarParam.DistanceHeuristic, value = DefaultValue },
                new KeyValuePairS { key = AstarParam.HeightHeuristic, value = DefaultValue },
                new KeyValuePairS { key = AstarParam.SlopeHeuristic, value = DefaultValue }
            });

        // Escalado de Coste y Heurística
        // Ajusta la penalización o recompensa de cada parámetro

        // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

        // =================== Coste ===================
        // Penaliza la Distancia Recorrida (Ruta + corta)
        public float DistanceCost
        {
            get => parameters.GetValue(AstarParam.DistanceCost);
            set => parameters.SetValue(AstarParam.DistanceCost, value);
        }

        // Penaliza cambiar la altura (evita que intente subir escalones grandes)
        public float HeightCost
        {
            get => parameters.GetValue(AstarParam.HeightCost);
            set => parameters.SetValue(AstarParam.HeightCost, value);
        }

        // Penaliza giros bruscos 
        public float TurnCost
        {
            get => parameters.GetValue(AstarParam.TurnCost);
            set => parameters.SetValue(AstarParam.TurnCost, value);
        }

        // =================== Heurística ===================
        // Recompensa acercarse al objetivo
        public float DistanceHeuristic
        {
            get => parameters.GetValue(AstarParam.DistanceHeuristic);
            set => parameters.SetValue(AstarParam.DistanceHeuristic, value);
        }

        // Recompensa acercarse a la altura del objetivo
        public float HeightHeuristic
        {
            get => parameters.GetValue(AstarParam.HeightHeuristic);
            set => parameters.SetValue(AstarParam.HeightHeuristic, value);
        }

        // Recompensa minimizar la pendiente (rodea montículos si puede)
        public float SlopeHeuristic
        {
            get => parameters.GetValue(AstarParam.SlopeHeuristic);
            set => parameters.SetValue(AstarParam.SlopeHeuristic, value);
        }
    }
}