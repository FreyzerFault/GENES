using UnityEngine;

namespace PathFinding.A_Star
{
    // ALGORTIMO A*
    public class AstarDirectional : Astar
    {
        private static AstarDirectional _instance;
        public new static AstarDirectional Instance => _instance ??= new AstarDirectional();

        public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig)
        {
            return FindPathAstar(start, end, terrain, paramsConfig, CalculateCost, base.CalculateHeuristic);
        }

        // ==================== COSTE Y HEURÍSTICA ====================

        protected override float CalculateCost(Node a, Node b, PathFindingConfigSO paramsConfig)
        {
            // Coste de GIRO = Ángulo entre la dirección para ir a b y la dirección con la que viene del parent de a
            var turn = Vector2.Angle(a.direction, Node.Direction(a, b)) * Mathf.Deg2Rad;
            var turnCost = turn * paramsConfig.aStarConfigDirectional.turnCost;

            return base.CalculateCost(a, b, paramsConfig) + turnCost;
        }

        // Misma Heuristica
    }
}