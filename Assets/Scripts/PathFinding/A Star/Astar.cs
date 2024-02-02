using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathFinding.A_Star
{
    // ALGORTIMO A*
    public class Astar : PathFindingAlgorithm
    {
        private static AstarDirectional _instance;
        public static AstarDirectional Instance => _instance ??= new AstarDirectional();

        public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig) =>
            FindPathAstar(start, end, terrain, paramsConfig, CalculateCost, CalculateHeuristic);

        // Permite inyectar la Funcion de Coste y de Heuristica
        protected Path FindPathAstar(
            Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig,
            Func<Node, Node, PathFindingConfigSO, float> costFunction,
            Func<Node, Node, PathFindingConfigSO, float> heuristicFunction)
        {
            // Si el Path de Cache tiene mismo inicio y fin => Devolverlo
            if (paramsConfig.useCache && IsCached(start, end)) return Cache.path;

            if (!IsLegal(start, paramsConfig) || !IsLegal(end, paramsConfig)) return new Path(start, end);

            var iterations = 0;

            // Nodes checked && Nodes to check
            var allNodes = new HashSet<Node> { start };
            var openNodes = new List<Node> { start };
            var exploredNodes = new List<Node>();

            // Main loop
            while (openNodes.Count > 0)
            {
                iterations++;

                // Select BEST Node
                var currentNode = GetBetterNode(openNodes);

                // Marcar como explorado
                openNodes.Remove(currentNode);
                exploredNodes.Add(currentNode);

                // Si cumple la condición objetivo => terminar algoritmo
                // End no tiene por qué ser un nodo que cuadre en la malla
                // Por lo que el último Nodo será el que se acerque a End colisionando con él
                var endReached = currentNode.Collision(end);
                if (iterations >= paramsConfig.maxIterations || endReached)
                {
                    // Como el Nodo actual esta demasiado cerca, lo ignoramos y conectamos el anterior a END
                    var lastNode = currentNode.Parent;
                    end.G = currentNode.G + costFunction(lastNode, end, paramsConfig);
                    end.H = 0;
                    end.Parent = lastNode;

                    var path = new Path(start, end);

                    if (paramsConfig.useCache) Cache.path = path;

                    path.ExploredNodes = exploredNodes.ToArray();
                    path.OpenNodes = openNodes.ToArray();

                    return path;
                }

                // Crear vecinos si es la 1º vez que se exploran
                if (currentNode.Neighbours == null || currentNode.Neighbours.Length == 0)
                    currentNode.Neighbours = CreateNeighbours(
                        currentNode, paramsConfig, terrain, allNodes
                    );

                // Explorar vecinos
                foreach (var neighbour in currentNode.Neighbours)
                {
                    if (!neighbour.Legal) continue;

                    var explored = exploredNodes.Contains(neighbour);
                    var opened = openNodes.Contains(neighbour);

                    // Ya explorado
                    if (explored) continue;

                    // Calcular coste
                    var newCost = currentNode.G + costFunction(currentNode, neighbour, paramsConfig);

                    // Si no está en la lista de exploración o su coste MEJORA, actualizar
                    if (newCost < neighbour.G || !opened)
                    {
                        // Actualizar coste y Heurística
                        neighbour.G = newCost;
                        neighbour.H = heuristicFunction(neighbour, end, paramsConfig);

                        // Conectar nodos
                        neighbour.Parent = currentNode;

                        if (!opened) openNodes.Add(neighbour);
                    }
                }
            }

            return Path.EmptyPath;
        }

        // ==================== COSTE Y HEURÍSTICA ====================
        protected override float CalculateCost(Node a, Node b, PathFindingConfigSO paramsConfig)
        {
            var distanceCost = a.Distance2D(b) * paramsConfig.aStarConfig.distanceCost;
            var heightCost = Math.Abs(a.Position.y - b.Position.y) * paramsConfig.aStarConfig.heightCost;

            return distanceCost + heightCost;
        }

        protected override float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig)
        {
            var distHeuristic = node.Distance2D(end) * paramsConfig.aStarConfig.distanceHeuristic;
            var heightHeuristic =
                Mathf.Abs(node.Position.y - end.Position.y) * paramsConfig.aStarConfig.heightHeuristic;

            var slopeHeuristic = node.SlopeAngle * paramsConfig.aStarConfig.slopeHeuristic;

            return distHeuristic + heightHeuristic + slopeHeuristic;
        }

        // Minimiza la Funcion objetivo
        // Si hay + de 1 Nodo con mismma F => Devuelve el que tenga menor H
        protected static Node GetBetterNode(List<Node> nodes)
        {
            // Get Min F Nodes
            var minF = nodes.Min(node => node.F);
            var minFunctionNodes = nodes.Where(node => node.F - minF < float.Epsilon).ToList();

            if (minFunctionNodes.Count == 1) return minFunctionNodes[0];

            // Get Min H Nodes
            var minH = minFunctionNodes.Min(node => node.H);
            minFunctionNodes = minFunctionNodes.Where(node => node.H - minH < float.Epsilon).ToList();

            return minFunctionNodes[0];
        }
    }
}