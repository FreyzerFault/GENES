using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using MyBox;
using UnityEngine;

namespace Map.PathFinding
{
    public enum PathFindingAlgorithmType
    {
        Astar,
        AstarDirectional,

        Dijkstra
        // BreadthFirstSearch,
        // DepthFirstSearch,
        // GreedyBestFirstSearch
        // RRT
        // RRT*
    }

    public abstract class PathFindingAlgorithm
    {
        // Principal algoritmo
        public abstract Path FindPath(
            Node start,
            Node end,
            UnityEngine.Terrain terrain,
            PathFindingConfigSo paramsConfig
        );

        // Ejecutar el Algoritmo para varios checkpoints
        public Path FindPathByCheckpoints(
            Node[] checkPoints,
            UnityEngine.Terrain terrain,
            PathFindingConfigSo paramsConfig
        )
        {
            if (checkPoints.Length < 2) return Path.EmptyPath;

            if (checkPoints.Length == 2) return FindPath(checkPoints[0], checkPoints[1], terrain, paramsConfig);

            var nodes = Array.Empty<Node>();
            for (var i = 1; i < checkPoints.Length; i++)
                nodes = nodes
                    .Concat(
                        FindPath(checkPoints[i - 1], checkPoints[i], terrain, paramsConfig).Nodes
                    )
                    .ToArray();

            var path = new Path(nodes);
            return path;
        }

        // NODE Parameters
        protected abstract float CalculateCost(Node a, Node b, PathFindingConfigSo paramsConfig);

        protected abstract float CalculateHeuristic(
            Node node,
            Node end,
            PathFindingConfigSo paramsConfig
        );

        // ==================== RESTRICCIONES ====================
        public bool IsLegal(Node node, PathFindingConfigSo paramsConfig)
        {
            bool legalHeight = LegalHeight(node.Height, paramsConfig),
                legalSlope = LegalSlope(node.slopeAngle, paramsConfig),
                legalPosition = LegalPosition(node.Pos2D, Terrain.activeTerrain);

            node.Legal = legalHeight && legalSlope && legalPosition;

            return node.Legal;
        }

        protected bool LegalPosition(Vector2 pos, Terrain terrain) => !terrain.OutOfBounds(pos);

        protected bool LegalHeight(float height, PathFindingConfigSo paramsConfig) =>
            height >= paramsConfig.minHeight;

        protected bool LegalSlope(float slopeAngle, PathFindingConfigSo paramsConfig) =>
            slopeAngle <= paramsConfig.maxSlopeAngle;

        // ==================== NEIGHBOURS ====================
        protected Node[] CreateNeighbours(
            Node node,
            PathFindingConfigSo paramsConfig,
            Terrain terrain,
            HashSet<Node> nodesAlreadyFound,
            bool onlyFrontNeighbours = true
        )
        {
            var neighbours = new List<Node>();
            var direction = node.direction;
            for (var i = 0; i < 9; i++)
            {
                if (i == 4) continue; // Skip central Node

                // Offset from central Node
                var offset = new Vector2(
                    node.size * (i % 3 - 1),
                    node.size * Mathf.Floor(i / 3f - 1)
                );

                // Only Front Neighbours
                if (onlyFrontNeighbours && Vector2.Dot(offset, direction) < 0) continue;

                // World Position
                var neighPos = new Vector3(
                    node.position.x + offset.x,
                    0,
                    node.position.z + offset.y
                );

                var neigh = new Node(neighPos);

                // 1º Check if already exists
                var foundIndex = nodesAlreadyFound.FirstIndex(neigh.Equals);
                if (foundIndex > -1)
                {
                    neighbours.Add(nodesAlreadyFound.ElementAt(foundIndex));
                    continue;
                }

                // Properties
                neigh.position = new Vector3(
                    neigh.position.x,
                    terrain.SampleHeight(neighPos),
                    neigh.position.z
                );
                neigh.slopeAngle = terrain.GetSlopeAngle(neighPos);
                neigh.size = node.size;

                // Si no es legal se ignora
                if (!IsLegal(neigh, paramsConfig)) continue;

                neighbours.Add(neigh);
                nodesAlreadyFound.Add(neigh);
            }

            return node.Neighbours = neighbours.ToArray();
        }

        #region CACHE

        protected struct PathFindingCache
        {
            public Path path;
        }

        protected PathFindingCache Cache;

        protected bool IsCached(Node start, Node end) =>
            Cache.path != null
            && Cache.path.Nodes.Length > 0
            && Cache.path.Start.Equals(start)
            && Cache.path.End.Equals(end);

        public void CleanCache()
        {
            Cache.path = Path.EmptyPath;
        }

        #endregion
    }
}