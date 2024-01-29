using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    // TODO Mover Path a su propio archivo
    [Serializable]
    public class Path
    {
        public static Path EmptyPath = new(Array.Empty<Node>());
        [NonSerialized] private Node[] _nodes;

        public Path(Node start, Node end)
        {
            _nodes = ExtractPath(start, end);
        }

        public Path(Node[] nodes)
        {
            _nodes = nodes;
        }

        public Path(Vector3[] points)
        {
            _nodes = points
                .Select(point => new Node(point))
                .ToArray();
        }

        public Node Start => _nodes.Length > 0 ? _nodes[0] : null;
        public Node End => _nodes.Length > 0 ? _nodes[^1] : null;

        public Node[] Nodes
        {
            get => _nodes;
            set => _nodes = value;
        }

        public bool IsEmpty => _nodes.Length == 0;

        public int NodeCount => _nodes.Length;

        private static Node[] ExtractPath(Node start, Node end)
        {
            // From end to start
            var path = new List<Node> { end };

            var currentNode = end.Parent;
            while (currentNode != null && !currentNode.Equals(start))
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
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

        public Vector2[] GetPathNormalizedPoints(Terrain terrain)
        {
            return _nodes.Select(node => terrain.GetNormalizedPosition(node.Position)).ToArray();
        }


        #region DEBUG INFO

        // FOR DEBUGGING
        [NonSerialized] public Node[] exploredNodes = Array.Empty<Node>();
        [NonSerialized] public Node[] openNodes = Array.Empty<Node>();

        #endregion
    }

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

        // NEIGHBOURS
        protected abstract Node[] CreateNeighbours(Node node, Terrain terrain, Node[] nodesAlreadyFound);

        // RESTRICCIONES
        protected abstract bool IsLegal(Node node, PathFindingConfigSO paramsConfig);
        protected abstract bool LegalPosition(Vector2 pos, Terrain terrain);
        protected abstract bool LegalHeight(float height, PathFindingConfigSO paramsConfig);
        protected abstract bool LegalSlope(float slopeAngle, PathFindingConfigSO paramsConfig);
        protected abstract bool OutOfBounds(Vector2 pos, Terrain terrain);

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