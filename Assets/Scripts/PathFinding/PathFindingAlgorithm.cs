using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    public enum PathFindingAlgorithmType
    {
        Astar,

        Dijkstra
        // BreadthFirstSearch,
        // DepthFirstSearch,
        // GreedyBestFirstSearch
        // ...
    }

    [Serializable]
    public struct Path
    {
        public static Path EmptyPath = new(Array.Empty<Node>());
        [NonSerialized] private Node[] _nodes;

        public Path(Node start, Node end)
        {
            _nodes = ExtractPath(start, end);
            Start = start;
            End = end;
        }

        public Path(Node[] nodes)
        {
            _nodes = nodes;
            if (nodes.Length > 0)
            {
                Start = nodes[0];
                End = nodes[^1];
            }
            else
            {
                Start = null;
                End = null;
            }
        }

        public Node Start { get; private set; }
        public Node End { get; private set; }

        public Node[] Nodes
        {
            get => _nodes;
            set
            {
                Start = _nodes[0];
                End = _nodes[^1];
                _nodes = value;
            }
        }

        public int PathNodeCount => _nodes.Length;

        private static Node[] ExtractPath(Node start, Node end)
        {
            // From end to start
            var path = new List<Node> { end };

            var currentNode = end.parent;
            while (currentNode != null && !currentNode.Equals(start))
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            return path.ToArray();
        }

        public float GetPathLength()
        {
            float length = 0;
            for (var i = 1; i < _nodes.Length; i++)
                length += Vector3.Distance(_nodes[i - 1].Position, _nodes[i].Position);

            return length;
        }


        public Vector3[] GetPathWorldPoints()
        {
            return _nodes.Select(node => node.Position).ToArray();
        }

        public Vector2[] GetPathNormalizedPoints(TerrainData terrain)
        {
            return _nodes.Select(node => terrain.GetNormalizedPosition(node.Position)).ToArray();
        }
    }

    public abstract class PathFindingAlgorithm
    {
        // Principal algoritmo
        public abstract Path FindPath(Node start, Node end, Terrain terrain, PathFindingConfigSO paramsConfig);

        // Ejecutar el Algoritmo para varios checkpoints
        public Path FindPathByCheckpoints(Node[] checkPoints, Terrain terrain, PathFindingConfigSO paramsConfig)
        {
            if (checkPoints.Length < 2) return Path.EmptyPath;

            var nodes = Array.Empty<Node>();
            for (var i = 1; i < checkPoints.Length; i++)
                nodes = nodes
                    .Concat(FindPath(checkPoints[i - 1], checkPoints[i], terrain, paramsConfig).Nodes)
                    .ToArray();
            return new Path(nodes);
        }


        // NODE Parameters
        protected abstract float CalculateCost(Node a, Node b, PathFindingConfigSO paramsConfig);
        protected abstract float CalculateHeuristic(Node node, Node end, PathFindingConfigSO paramsConfig);
        protected abstract bool IsLegal(Node node, PathFindingConfigSO paramsConfig);

        // NEIGHBOURS
        protected abstract Node[] CreateNeighbours(Node node, Terrain terrain);

        // RESTRICCIONES
        protected abstract bool OutOfBounds(Vector2 pos, Terrain terrain);
        protected abstract bool IllegalPosition(Vector2 pos, Terrain terrain);

        #region CACHE

        protected Path CachedPath = Path.EmptyPath;

        protected bool IsCached(Node start, Node end)
        {
            return CachedPath.Nodes.Length > 0 &&
                   CachedPath.Start.Equals(start) &&
                   CachedPath.End.Equals(end);
        }

        public void CleanCache()
        {
            CachedPath = Path.EmptyPath;
        }

        #endregion
    }
}