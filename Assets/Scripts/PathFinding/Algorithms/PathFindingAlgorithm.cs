using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using MyBox;
using PathFinding.Settings;
using UnityEngine;

namespace PathFinding.Algorithms
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
        public abstract Path FindPath(Node start, Node end, Terrain terrain, Bounds bounds, PathFindingSettings settings);

        // Ejecutar el Algoritmo para varios checkpoints
        public Path FindPathByCheckpoints(Node[] checkPoints, Terrain terrain, Bounds bounds, PathFindingSettings pathFindingSettings)
        {
            switch (checkPoints.Length)
            {
                case < 2:
                    return Path.EmptyPath;
                case 2:
                    return FindPath(checkPoints[0], checkPoints[1], terrain, bounds, pathFindingSettings);
                case > 2:
                    var nodes = Array.Empty<Node>();
                    for (var i = 1; i < checkPoints.Length; i++)
                        nodes = nodes.Concat(
                                FindPath(checkPoints[i - 1], checkPoints[i], terrain, bounds, pathFindingSettings).nodes
                            ).ToArray();

                    var path = new Path(nodes);
                    return path;
            }
        }

        #region RESTRICTIONS

        protected bool IsLegal(Node node, PathFindingSettings settings, Bounds bounds)
        {
            bool legalHeight = settings.LegalHeight(node.Height),
                legalSlope = settings.LegalSlope(node.slopeAngle),
                legalPosition = bounds.Contains(node.position);

            node.Legal = legalHeight && legalSlope && legalPosition;

            return node.Legal;
        }

        #endregion
        
        // ==================== NEIGHBOURS ====================
        protected Node[] CreateNeighbours(Node node, PathFindingSettings settings, Terrain terrain, Bounds bounds, HashSet<Node> nodesAlreadyFound, bool onlyFrontNeighbours = true)
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
                if (!IsLegal(neigh, settings, bounds)) continue;

                neighbours.Add(neigh);
                nodesAlreadyFound.Add(neigh);
            }

            return node.neighbours = neighbours.ToArray();
        }

        #region CACHE

        public Dictionary<Tuple<Node, Node>, Path> cache = new();

        protected bool IsCached(Node start, Node end) =>
            cache.ContainsKey(new Tuple<Node, Node>(start, end));
        
        protected Path GetCached(Node start, Node end) =>
            IsCached(start, end) ? cache[new Tuple<Node, Node>(start, end)] : null;
        
        protected void StoreInCache(Node start, Node end, Path path) =>
            cache[new Tuple<Node, Node>(start, end)] = path;

        public void CleanCache() => cache.Clear();

        #endregion
    }
}