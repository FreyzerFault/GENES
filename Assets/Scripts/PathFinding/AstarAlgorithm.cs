using System;
using System.Collections.Generic;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    [Serializable]
    public struct AstarConfig
    {
        // Escalado de Coste y Heurística
        // Ajusta la penalización o recompensa de cada parámetro

        // TRAYECTO MÁS CORTO vs TERRENO MÁS SEGURO

        // =================== Coste ===================
        // Penaliza la Distancia Recorrida (Ruta + corta)
        public float distanceCost;

        // Penaliza cambiar la altura (evita que intente subir escalones grandes)
        public float heightCost;

        // =================== Heurística ===================
        // Recompensa acercarse al objetivo
        public float distanceHeuristic;

        // Recompensa acercarse a la altura del objetivo
        public float heightHeuristic;

        // Recompensa minimizar la pendiente (rodea montículos si puede)
        public float slopeHeuristic;
        public float maxSlopeAngle;
    }

    // TODO => Añadir Coste de Giro
    // (Necesito la dirección del Nodo Inicial y cada Nodo necesitará saber su dirección)

    // ALGORTIMO A*
    public class AstarAlgorithm : PathFindingAlgorithm
    {
        private static AstarAlgorithm _instance;
        public static AstarAlgorithm Instance => _instance ??= new AstarAlgorithm();

        public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig)
        {
            // Si el Path de Cache tiene mismo inicio y fin => Devolverlo
            if (paramsConfig.useCache && IsCached(start, end))
                return CachedPath;

            if (!IsLegal(start, paramsConfig) || !IsLegal(end, paramsConfig)) return Path.EmptyPath;

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
                    end.parent = currentNode;

                    var path = new Path(start, end);

                    if (paramsConfig.useCache) CachedPath = path;

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
                        neighbour.parent = currentNode;

                        if (!openList.Contains(neighbour)) openList.Add(neighbour);
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

            return distanceCost + heightCost;
        }

        protected override float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig)
        {
            var distHeuristic = Vector2.Distance(node.Pos2D, end.Pos2D) * paramsConfig.aStarConfig.distanceHeuristic;
            var heightHeuristic =
                Mathf.Abs(node.Position.y - end.Position.y) * paramsConfig.aStarConfig.heightHeuristic;

            var slopeHeuristic = node.SlopeAngle * paramsConfig.aStarConfig.slopeHeuristic;

            return distHeuristic + heightHeuristic + slopeHeuristic;
        }

        protected override bool IsLegal(Node node, PathFindingConfigSO paramsConfig)
        {
            bool legalHeight = node.Height >= paramsConfig.minHeight,
                legalSlope = node.SlopeAngle <= paramsConfig.aStarConfig.maxSlopeAngle;

            return legalHeight && legalSlope;
        }

        // ==================== VECINOS ====================
        protected override Node[] CreateNeighbours(Node node, Terrain terrain)
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

                if (node.parent != null && node.Equals(new Node(neighPos, size: node.Size)))
                {
                    neighbours.Add(node.parent);
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
        protected override bool OutOfBounds(Vector2 pos, Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var terrainPos = terrain.GetPosition();
            Vector2 lowerBound = new(terrainPos.x, terrainPos.z);
            var upperBound = lowerBound + new Vector2(terrainData.size.x, terrainData.size.z);
            bool overLowerBound = pos.x > lowerBound.x && pos.y > lowerBound.y,
                underUpperBound = pos.x < upperBound.x && pos.y < upperBound.y;

            return !overLowerBound && underUpperBound;
        }

        protected override bool IllegalPosition(Vector2 pos, Terrain terrain)
        {
            return OutOfBounds(pos, terrain);
        }
    }
}