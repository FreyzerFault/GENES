using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using MyBox;
using UnityEngine;

namespace PathFinding
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
        public abstract Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig);

        // Ejecutar el Algoritmo para varios checkpoints
        public Path FindPathByCheckpoints(Node[] checkPoints, Terrain terrain, PathFindingConfigSO paramsConfig)
        {
            if (checkPoints.Length < 2) return Path.EmptyPath;

            if (checkPoints.Length == 2) return FindPath(checkPoints[0], checkPoints[1], terrain, paramsConfig);

            var nodes = Array.Empty<Node>();
            for (var i = 1; i < checkPoints.Length; i++)
                nodes = nodes
                    .Concat(FindPath(checkPoints[i - 1], checkPoints[i], terrain, paramsConfig).Nodes)
                    .ToArray();

            var path = new Path(nodes);
            return path;
        }


        // NODE Parameters
        protected abstract float CalculateCost(Node a, Node b, PathFindingConfigSO paramsConfig);
        protected abstract float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig);


        // ==================== RESTRICCIONES ====================
        protected bool IsLegal(Node node, PathFindingConfigSO paramsConfig)
        {
            bool legalHeight = LegalHeight(node.Height, paramsConfig),
                legalSlope = LegalSlope(node.SlopeAngle, paramsConfig),
                legalPosition = LegalPosition(node.Pos2D, Terrain.activeTerrain);

            node.Legal = legalHeight && legalSlope && legalPosition;

            return node.Legal;
        }

        protected bool LegalPosition(Vector2 pos, Terrain terrain)
        {
            return !terrain.OutOfBounds(pos);
        }

        protected bool LegalHeight(float height, PathFindingConfigSO paramsConfig)
        {
            return height >= paramsConfig.minHeight;
        }

        protected bool LegalSlope(float slopeAngle, PathFindingConfigSO paramsConfig)
        {
            return slopeAngle <= paramsConfig.aStarConfig.maxSlopeAngle;
        }

        // ==================== NEIGHBOURS ====================
        protected Node[] CreateNeighbours(Node node, PathFindingConfigSO paramsConfig, Terrain terrain,
            HashSet<Node> nodesAlreadyFound, bool onlyFrontNeighbours = true)
        {
            var neighbours = new List<Node>();
            var direction = node.direction;
            for (var i = 0; i < 9; i++)
            {
                if (i == 4) continue; // Skip central Node

                // Offset from central Node
                var offset = new Vector2(
                    node.Size * (i % 3 - 1),
                    node.Size * Mathf.Floor(i / 3f - 1)
                );

                // Only Front Neighbours
                if (onlyFrontNeighbours && Vector2.Dot(offset, direction) < 0)
                    continue;

                // World Position
                var neighPos = new Vector3(
                    node.Position.x + offset.x,
                    0,
                    node.Position.z + offset.y
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
                neigh.Position = new Vector3(neigh.Position.x, terrain.SampleHeight(neighPos), neigh.Position.z);
                neigh.SlopeAngle = terrain.GetSlopeAngle(neighPos);
                neigh.Size = node.Size;

                // Si no es legal se ignora
                if (!IsLegal(neigh, paramsConfig))
                    continue;

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

        protected bool IsCached(Node start, Node end)
        {
            return Cache.path.Nodes.Length > 0 &&
                   Cache.path.Start.Equals(start) &&
                   Cache.path.End.Equals(end);
        }

        public void CleanCache()
        {
            Cache.path = Path.EmptyPath;
        }

        #endregion
    }
}