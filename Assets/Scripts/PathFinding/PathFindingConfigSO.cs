using System;
using MyBox;
using PathFinding.A_Star;
using UnityEngine;

namespace PathFinding
{
    [CreateAssetMenu(fileName = "PathFinding Config", menuName = "Configurations/PathFinding Configuration", order = 1)]
    public class PathFindingConfigSO : ScriptableObject
    {
        // Distancia entre nodos
        // A + grandes = + rápido pero + impreciso
        public float cellSize = 1f;

        // Maximas iteraciones del algoritmo
        public int maxIterations = 1000;


        // ===============================================================
        // RESTRICCIONES
        public float maxSlopeAngle = 30f;
        public float minHeight = 100f;


        // ===============================================================
        // Cache para evitar recalcular el camino
        public bool useCache = true;

        // ============================= ALGORITHM ==================================
        [SerializeField] public PathFindingAlgorithmType algorithm;

        [ConditionalField("algorithm", false, PathFindingAlgorithmType.Astar)]
        public AstarConfig aStarConfig;

        [ConditionalField("algorithm", false, PathFindingAlgorithmType.AstarDirectional)]
        public AstarConfig aStarConfigDirectional;

        public PathFindingAlgorithm Algorithm =>
            algorithm switch
            {
                PathFindingAlgorithmType.Astar => AstarDirectional.Instance,
                PathFindingAlgorithmType.AstarDirectional => AstarDirectional.Instance,
                PathFindingAlgorithmType.Dijkstra => DijkstraAlgorithm.Instance,
                _ => throw new ArgumentOutOfRangeException()
            };

        // ================================ FINE-TUNING ================================
        public float DistanceCost
        {
            get => aStarConfig.DistanceCost;
            set => aStarConfig.DistanceCost = value;
        }

        public float HeightCost
        {
            get => aStarConfig.HeightCost;
            set => aStarConfig.HeightCost = value;
        }

        public float TurnCost
        {
            get => aStarConfig.TurnCost;
            set => aStarConfig.TurnCost = value;
        }

        public float DistanceHeuristic
        {
            get => aStarConfig.DistanceHeuristic;
            set => aStarConfig.DistanceHeuristic = value;
        }

        public float HeightHeuristic
        {
            get => aStarConfig.HeightHeuristic;
            set => aStarConfig.HeightHeuristic = value;
        }

        public float SlopeHeuristic
        {
            get => aStarConfig.SlopeHeuristic;
            set => aStarConfig.SlopeHeuristic = value;
        }

        public event Action OnFineTune;
        public void OnFineTuneTriggerEvent() => OnFineTune?.Invoke();
    }
}