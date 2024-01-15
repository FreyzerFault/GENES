using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    // TODO => Añadir Coste de Giro
    // (Necesito la dirección del Nodo Inicial y cada Nodo necesitará saber su dirección)
    public static class AstarAlgorithm
    {
        // Cache
        public static Node[] cachedPath;

        public static void CleanCache()
        {
            cachedPath = Array.Empty<Node>();
        }

        // ALGORTIMO A*
        public static Node[] FindPath(Node start, Node end, Terrain terrain, AstarConfigSO paramsConfig)
        {
            // Cache de caminos para no repetir el mismo
            if (paramsConfig.useCache && cachedPath is { Length: > 0 } && cachedPath[0].Position == start.Position &&
                cachedPath[^1].Position == end.Position)
                return cachedPath;
            
            if (!IsLegal(start, paramsConfig) || !IsLegal(end, paramsConfig)) return Array.Empty<Node>();

            var iterations = 0;

            // Nodes checked
            List<Node> closedList = new();

            // Nodes to check
            List<Node> openList = new() { start };

            // Main loop
            while (openList.Count > 0)
            {
                iterations++;

                // Sort by F, then by H
                openList.Sort((a, b) =>
                {
                    bool betterFunction = a.F < b.F,
                        sameFunction = Math.Abs(a.F - b.F) < float.Epsilon,
                        betterHeuristic = a.H < b.H,
                        sameHeuristic = Math.Abs(a.H - b.H) < float.Epsilon;
                    return sameFunction ? sameHeuristic ? 0 : betterHeuristic ? -1 : 1 : betterFunction ? -1 : 1;
                });

                // Select BEST Node
                var currentNode = openList[0];

                // Marcar como explorado
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // Si cumple la condición objetivo, terminar
                // End no tiene por qué ser un nodo que cuadre en la malla
                // El último Nodo será el que se acerque a End hasta que su tamaño lo contenga 
                if (iterations >= paramsConfig.maxIterations ||
                    Vector2.Distance(currentNode.Pos2D, end.Pos2D) < currentNode.Size)
                {
                    end.G = CalculateCost(currentNode, end, paramsConfig);
                    end.H = 0;
                    end.Parent = currentNode;

                    var path = GetPath(start, end);

                    if (paramsConfig.useCache) cachedPath = path;

                    return path;
                }

                // Crear vecinos si es la 1º vez que se exploran
                if (currentNode.Neighbours == null || currentNode.Neighbours.Length == 0)
                    currentNode.Neighbours = CreateNeighbours(currentNode, terrain);

                // Explorar vecinos
                foreach (var neighbour in currentNode.Neighbours)
                {
                    neighbour.Legal = IsLegal(neighbour, paramsConfig);
                    if (!neighbour.Legal) continue;

                    // Ya explorado
                    if (closedList.Contains(neighbour)) continue;

                    // Calcular coste
                    var moveCost = CalculateCost(currentNode, neighbour, paramsConfig);

                    // Si no está en la lista de exploración o su coste MEJORA, actualizar
                    if (moveCost < neighbour.G || !openList.Contains(neighbour))
                    {
                        // Actualizar coste y Heurística
                        neighbour.G = moveCost;
                        neighbour.H = CalculateHeuristic(neighbour, end, paramsConfig);

                        // Conectar nodos
                        neighbour.Parent = currentNode;

                        if (!openList.Contains(neighbour)) openList.Add(neighbour);
                    }
                }
            }

            return Array.Empty<Node>();
        }

        public static Node[] FindPathByCheckpoints(Node[] checkPoints, Terrain terrain, AstarConfigSO paramsConfig)
        {
            var pathNodes = Array.Empty<Node>();
            for (var i = 1; i < checkPoints.Length; i++)
                pathNodes = pathNodes.Concat(FindPath(checkPoints[i - 1], checkPoints[i], terrain, paramsConfig))
                    .ToArray();
            return pathNodes;
        }

        // ==================== COSTE Y HEURÍSTICA ====================
        private static float CalculateCost(Node a, Node b, AstarConfigSO paramsConfig)
        {
            var distanceCost = Vector2.Distance(a.Pos2D, b.Pos2D) * paramsConfig.distanceCost;
            var heightCost = Math.Abs(a.Position.y - b.Position.y) * paramsConfig.heightCost;

            return distanceCost + heightCost;
        }

        private static float CalculateHeuristic(Node node, Node end, AstarConfigSO paramsConfig)
        {
            var distHeuristic = Vector2.Distance(node.Pos2D, end.Pos2D) * paramsConfig.distanceHeuristic;
            var heightHeuristic = Mathf.Abs(node.Position.y - end.Position.y) * paramsConfig.heightHeuristic;

            var slopeHeuristic = node.SlopeAngle * paramsConfig.slopeHeuristic;

            return distHeuristic + heightHeuristic + slopeHeuristic;
        }

        private static bool IsLegal(Node node, AstarConfigSO paramsConfig)
        {
            bool legalHeight = node.Height >= paramsConfig.minHeight,
                legalSlope = node.SlopeAngle <= paramsConfig.maxSlopeAngle;

            return legalHeight && legalSlope;
        }

        // ==================== VECINOS ====================
        private static Node[] CreateNeighbours(Node node, Terrain terrain)
        {
            var neighbours = new List<Node>();
            for (var i = 0; i < 8; i++)
            {
                // Offset from central Node
                var xOffset = node.Size * (i % 3 - 1);
                var zOffset = node.Size * Mathf.Floor(i / 3f - 1);

                // World Position
                var neighPos = new Vector3(
                    node.Position.x + xOffset,
                    0,
                    node.Position.z + zOffset
                );

                if (node.Parent != null && node.Equals(new Node(neighPos, 0, node.Size)))
                {
                    neighbours.Add(node.Parent);
                    continue;
                }

                // Check OUT OF BOUNDS of Terrain
                if (OutOfBounds(new Vector2(neighPos.x, neighPos.z), terrain))
                    continue;

                // Terrain Height
                neighPos.y = terrain.SampleHeight(neighPos);

                // Slope Angle
                var slopeAngle = terrain.GetSlopeAngle(neighPos);

                neighbours.Add(new Node(neighPos, slopeAngle, node.Size));
            }

            node.Neighbours = neighbours.ToArray();
            return node.Neighbours;
        }

        // ==================== RESTRICCIONES ====================
        private static bool OutOfBounds(Vector2 pos, Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var terrainPos = terrain.GetPosition();
            Vector2 lowerBound = new(terrainPos.x, terrainPos.z);
            var upperBound = lowerBound + new Vector2(terrainData.size.x, terrainData.size.z);
            bool overLowerBound = pos.x > lowerBound.x && pos.y > lowerBound.y,
                underUpperBound = pos.x < upperBound.x && pos.y < upperBound.y;

            return !overLowerBound && underUpperBound;
        }

        private static bool IllegalPosition(Vector2 pos, Terrain terrain)
        {
            return OutOfBounds(pos, terrain);
        }

        // ==================== TRAYECTO FINAL ====================
        private static Node[] GetPath(Node start, Node end)
        {
            // From end to start
            var path = new List<Node> { end };

            var currentNode = end.Parent;
            while (currentNode != start && currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            path.Add(start);
            path.Reverse();
            return path.ToArray();
        }

        public static float GetPathLength(Node[] path)
        {
            float length = 0;
            for (var i = 1; i < path.Length; i++)
                length += Vector3.Distance(path[i - 1].Position, path[i].Position);

            return length;
        }

        public static Vector3[] GetPathWorldPoints(Node[] path)
        {
            if (path.Length == 0) return Array.Empty<Vector3>();
            return path.Select(node => node.Position).ToArray();
        }

        public static Vector2[] GetPathNormalizedPoints(Node[] path, TerrainData terrain)
        {
            return path.Select(node => terrain.GetNormalizedPosition(node.Position)).ToArray();
        }
    }
}