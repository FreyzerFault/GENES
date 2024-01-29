using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding.A_Star
{
    // TODO => Añadir Coste de Giro
    // (Necesito la dirección del Nodo Inicial y cada Nodo necesitará saber su dirección)

    // ALGORTIMO A*
    public class Astar : PathFindingAlgorithm
    {
        private static AstarDirectional _instance;
        public static AstarDirectional Instance => _instance ??= new AstarDirectional();

        public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig)
        {
            // Si el Path de Cache tiene mismo inicio y fin => Devolverlo
            if (paramsConfig.useCache && IsCached(start, end)) return Cache.path;

            if (!IsLegal(start, paramsConfig) || !IsLegal(end, paramsConfig)) return Path.EmptyPath;

            var iterations = 0;

            // Nodes checked && Nodes to check
            var exploredNodes = new List<Node>();
            var openNodes = new List<Node> { start };

            // Main loop
            while (openNodes.Count > 0)
            {
                iterations++;

                // Sort by F, then by H
                openNodes.Sort((a, b) =>
                {
                    bool betterFunction = a.F < b.F,
                        sameFunction = Math.Abs(a.F - b.F) < float.Epsilon,
                        betterHeuristic = a.H < b.H,
                        sameHeuristic = Math.Abs(a.H - b.H) < float.Epsilon;
                    return sameFunction ? sameHeuristic ? 0 : betterHeuristic ? -1 : 1 : betterFunction ? -1 : 1;
                });

                // Select BEST Node
                var currentNode = openNodes[0];

                // Marcar como explorado
                openNodes.Remove(currentNode);
                exploredNodes.Add(currentNode);

                // Si cumple la condición objetivo, terminar
                // End no tiene por qué ser un nodo que cuadre en la malla
                // El último Nodo será el que se acerque a End hasta que su tamaño lo contenga 
                if (iterations >= paramsConfig.maxIterations ||
                    Vector2.Distance(currentNode.Pos2D, end.Pos2D) < currentNode.Size)
                {
                    end.G = CalculateCost(currentNode, end, paramsConfig);
                    end.H = 0;
                    end.Parent = currentNode;

                    var path = new Path(start, end);

                    if (paramsConfig.useCache) Cache.path = path;

                    path.exploredNodes = exploredNodes.ToArray();
                    path.openNodes = openNodes.ToArray();

                    return path;
                }

                // Crear vecinos si es la 1º vez que se exploran
                if (currentNode.Neighbours == null || currentNode.Neighbours.Length == 0)
                    currentNode.Neighbours =
                        CreateNeighbours(currentNode, terrain, openNodes.Concat(exploredNodes).ToArray());

                // Explorar vecinos
                foreach (var neighbour in currentNode.Neighbours)
                {
                    neighbour.Legal = IsLegal(neighbour, paramsConfig);
                    if (!neighbour.Legal) continue;

                    var explored = exploredNodes.Exists(node => node.Equals(neighbour));
                    var opened = openNodes.Exists(node => node.Equals(neighbour));

                    // Ya explorado
                    if (explored) continue;

                    // Calcular coste
                    var moveCost = CalculateCost(currentNode, neighbour, paramsConfig);

                    // Si no está en la lista de exploración o su coste MEJORA, actualizar
                    if (moveCost < neighbour.G || !opened)
                    {
                        // Actualizar coste y Heurística
                        neighbour.G = moveCost;
                        neighbour.H = CalculateHeuristic(neighbour, end, paramsConfig);

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
            var distanceCost = Vector2.Distance(a.Pos2D, b.Pos2D) * paramsConfig.aStarConfig.distanceCost;
            var heightCost = Math.Abs(a.Position.y - b.Position.y) * paramsConfig.aStarConfig.heightCost;

            return a.G + distanceCost + heightCost;
        }

        protected override float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig)
        {
            var distHeuristic = Vector2.Distance(node.Pos2D, end.Pos2D) * paramsConfig.aStarConfig.distanceHeuristic;
            var heightHeuristic =
                Mathf.Abs(node.Position.y - end.Position.y) * paramsConfig.aStarConfig.heightHeuristic;

            var slopeHeuristic = node.SlopeAngle * paramsConfig.aStarConfig.slopeHeuristic;

            return distHeuristic + heightHeuristic + slopeHeuristic;
        }


        // ==================== VECINOS ====================
        protected override Node[] CreateNeighbours(Node node, Terrain terrain, Node[] nodesAlreadyFound)
        {
            var neighbours = new List<Node>();
            for (var i = 0; i < 9; i++)
            {
                if (i == 4) continue; // Skip central Node

                // Offset from central Node
                var xOffset = node.Size * (i % 3 - 1);
                var zOffset = node.Size * Mathf.Floor(i / 3f - 1);

                // World Position
                var neighPos = new Vector3(
                    node.Position.x + xOffset,
                    0,
                    node.Position.z + zOffset
                );

                var neigh = new Node(neighPos);

                // 1º Check if already exists
                var foundIndex = nodesAlreadyFound.ToList().FindIndex(node.Equals);
                if (foundIndex != -1)
                {
                    neighbours.Add(nodesAlreadyFound[foundIndex]);
                    continue;
                }

                // Check OUT OF BOUNDS of Terrain
                if (OutOfBounds(new Vector2(neighPos.x, neighPos.z), terrain))
                    continue;

                // It's NEW
                // Set other parameters and ADD to Neighbours

                // Terrain Height
                neigh.Position = new Vector3(neigh.Position.x, terrain.SampleHeight(neighPos), neigh.Position.z);

                // Slope Angle
                neigh.SlopeAngle = terrain.GetSlopeAngle(neighPos);
                neigh.Size = node.Size;

                neighbours.Add(neigh);
            }

            node.Neighbours = neighbours.ToArray();
            return node.Neighbours;
        }

        // ==================== RESTRICCIONES ====================

        protected override bool IsLegal(Node node, PathFindingConfigSO paramsConfig)
        {
            bool legalHeight = LegalHeight(node.Height, paramsConfig),
                legalSlope = LegalSlope(node.SlopeAngle, paramsConfig),
                legalPosition = LegalPosition(node.Pos2D, Terrain.activeTerrain);

            node.Legal = legalHeight && legalSlope && legalPosition;

            return node.Legal;
        }

        protected override bool LegalPosition(Vector2 pos, Terrain terrain)
        {
            return !OutOfBounds(pos, terrain);
        }

        protected override bool LegalHeight(float height, PathFindingConfigSO paramsConfig)
        {
            return height >= paramsConfig.minHeight;
        }

        protected override bool LegalSlope(float slopeAngle, PathFindingConfigSO paramsConfig)
        {
            return slopeAngle <= paramsConfig.aStarConfig.maxSlopeAngle;
        }

        protected override bool OutOfBounds(Vector2 pos, Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var terrainPos = terrain.GetPosition();

            // BOUNDS
            Vector2 lowerBound = new(terrainPos.x, terrainPos.z);
            var upperBound = lowerBound + new Vector2(terrainData.size.x, terrainData.size.z);

            bool overLowerBound = pos.x > lowerBound.x && pos.y > lowerBound.y,
                underUpperBound = pos.x < upperBound.x && pos.y < upperBound.y;

            return !(overLowerBound && underUpperBound);
        }
    }
}