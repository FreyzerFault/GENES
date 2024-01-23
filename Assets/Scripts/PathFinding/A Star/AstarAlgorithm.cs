using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using MyBox;
using UnityEngine;

namespace PathFinding.A_Star
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

        public override Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig,
            out List<Node> exploredNodes, out List<Node> openNodes)
        {
            exploredNodes = new List<Node>();
            openNodes = new List<Node>();

            // Si el Path de Cache tiene mismo inicio y fin => Devolverlo
            if (paramsConfig.useCache && IsCached(start, end))
            {
                exploredNodes = Cache.exploredNodes;
                openNodes = Cache.openNodes;
                return Cache.path;
            }

            if (!IsLegal(start, paramsConfig) || !IsLegal(end, paramsConfig)) return Path.EmptyPath;

            var iterations = 0;

            // Nodes checked
            exploredNodes = new List<Node>();

            // Nodes to check
            openNodes = new List<Node> { start };

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
                    end.parent = currentNode;

                    var path = new Path(start, end);

                    if (paramsConfig.useCache)
                    {
                        Cache.path = path;
                        Cache.exploredNodes = exploredNodes;
                        Cache.openNodes = openNodes;
                    }

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
                        neighbour.parent = currentNode;

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

        protected override bool IsLegal(Node node, PathFindingConfigSO paramsConfig)
        {
            bool legalHeight = node.Height >= paramsConfig.minHeight,
                legalSlope = node.SlopeAngle <= paramsConfig.aStarConfig.maxSlopeAngle;

            return legalHeight && legalSlope;
        }

        // ==================== VECINOS ====================
        protected override Node[] CreateNeighbours(Node node, Terrain terrain, Node[] nodesAlreadyFound)
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

                var neigh = new Node(neighPos);

                // 1º Check if already exists
                var foundIndex = nodesAlreadyFound.FirstIndex(node => node.Equals(neigh));
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