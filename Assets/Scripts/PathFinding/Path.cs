using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEngine;

namespace PathFinding
{
    [Serializable]
    public class Path
    {
        public static Path EmptyPath = new(Array.Empty<Node>());
        [SerializeField] private Node[] _nodes;

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

            path.Add(start);

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
        public Node[] ExploredNodes = Array.Empty<Node>();
        public Node[] OpenNodes = Array.Empty<Node>();

        #endregion
    }
}