using System.Collections.Generic;
using System.Linq;
using DavidUtils.Collections;
using UnityEngine;

namespace GENES.PathFinding.Settings.Astar
{

    [CreateAssetMenu(fileName = "Astar Parameters", menuName = "PathFinding/Astar Parameters")]
    public class AstarParameters: AlgorithmParams
    {
        protected override float[] DefaultValue => new [] { 1f, 1f, 1f, 1f, 1f };
        
        public override string[] DisplayNames => new [] { "Distance", "Height", "Distance", "Height", "Slope" };
        public override ParamType[] Types => new[]
        {
            ParamType.DistanceCost,
            ParamType.HeightCost,
            ParamType.DistanceHeuristic,
            ParamType.HeightHeuristic,
            ParamType.SlopeHeuristic
        };

        public AstarParameters(IDictionary<ParamType, ParamValue> parameters = null)
        {
            if (parameters != null)
                SetParameters(parameters);
            else
                SetDefaultValues();
        }


        // Escalado de Coste y Heurística
        // Ajusta la penalización o recompensa de cada parámetro

        // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

        // =================== Coste ===================
        // Penaliza la Distancia Recorrida (Ruta + corta)
        public float DistanceCost
        {
            get => GetValue(ParamType.DistanceCost);
            set => SetValue(ParamType.DistanceCost, value);
        }

        // Penaliza cambiar la altura (evita que intente subir escalones grandes)
        public float HeightCost
        {
            get => GetValue(ParamType.HeightCost);
            set => SetValue(ParamType.HeightCost, value);
        }

        // =================== Heurística ===================
        // Recompensa acercarse al objetivo
        public float DistanceHeuristic
        {
            get => GetValue(ParamType.DistanceHeuristic);
            set => SetValue(ParamType.DistanceHeuristic, value);
        }

        // Recompensa acercarse a la altura del objetivo
        public float HeightHeuristic
        {
            get => GetValue(ParamType.HeightHeuristic);
            set => SetValue(ParamType.HeightHeuristic, value);
        }

        // Recompensa minimizar la pendiente (rodea montículos si puede)
        public float SlopeHeuristic
        {
            get => GetValue(ParamType.SlopeHeuristic);
            set => SetValue(ParamType.SlopeHeuristic, value);
        }

    }
}
