using System;
using Map.PathFinding.A_Star;
using Map.PathFinding.Dijkstra;
using MyBox;
using UnityEngine;

namespace Map.PathFinding
{
    [CreateAssetMenu(fileName = "PathFinding Config", menuName = "Configurations/PathFinding Configuration", order = 1)]
    public class PathFindingConfigSo : ScriptableObject
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

        [ConditionalField("algorithm", false, PathFindingAlgorithmType.Dijkstra)]
        public AstarConfig dijkstraConfig;

        public PathFindingAlgorithm Algorithm =>
            algorithm switch
            {
                PathFindingAlgorithmType.Astar => Astar.Instance,
                PathFindingAlgorithmType.AstarDirectional => AstarDirectional.Instance,
                PathFindingAlgorithmType.Dijkstra => DijkstraAlgorithm.Instance,
                _ => throw new ArgumentOutOfRangeException()
            };

        public AstarConfig Config =>
            algorithm switch
            {
                PathFindingAlgorithmType.Astar => aStarConfig,
                PathFindingAlgorithmType.AstarDirectional => aStarConfigDirectional,
                PathFindingAlgorithmType.Dijkstra => dijkstraConfig,
                _ => throw new ArgumentOutOfRangeException()
            };

        // ================================ FINE-TUNING ================================
        public float DistanceCost
        {
            get => Config.DistanceCost;
            set => Config.DistanceCost = value;
        }

        public float HeightCost
        {
            get => Config.HeightCost;
            set => Config.HeightCost = value;
        }

        public float TurnCost
        {
            get => Config.TurnCost;
            set => Config.TurnCost = value;
        }

        public float DistanceHeuristic
        {
            get => Config.DistanceHeuristic;
            set => Config.DistanceHeuristic = value;
        }

        public float HeightHeuristic
        {
            get => Config.HeightHeuristic;
            set => Config.HeightHeuristic = value;
        }

        public float SlopeHeuristic
        {
            get => Config.SlopeHeuristic;
            set => Config.SlopeHeuristic = value;
        }

        public event Action OnFineTune;
        public void OnFineTuneTriggerEvent() => OnFineTune?.Invoke();
    }
}