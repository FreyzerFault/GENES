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

        [SerializeField] private Node[] nodes;

        public Path(Node start, Node end) => nodes = ExtractPath(start, end);

        public Path(Node[] nodes) => this.nodes = nodes;

        public Path(Vector3[] points)
        {
            nodes = points
                .Select(point => new Node(point))
                .ToArray();
        }

        public Node Start => nodes.Length > 0 ? nodes[0] : null;
        public Node End => nodes.Length > 0 ? nodes[^1] : null;

        public Node[] Nodes
        {
            get => nodes;
            set => nodes = value;
        }

        public bool IsEmpty => nodes.Length == 0;

        public int NodeCount => nodes.Length;

        public bool IsIllegal => !nodes[0].Legal || !nodes[^1].Legal;

        private static Node[] ExtractPath(Node start, Node end)
        {
            if (!start.Legal || !end.Legal) return new[] { start, end };

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
            for (var i = 1; i < nodes.Length; i++)
                length += Vector3.Distance(nodes[i - 1].Position, nodes[i].Position);

            return length;
        }


        public Vector3[] GetPathWorldPoints() =>
            nodes.Select(node => node.Position).ToArray();

        public Vector2[] GetPathNormalizedPoints(Terrain terrain) =>
            nodes.Select(node => terrain.GetNormalizedPosition(node.Position)).ToArray();


        public void ProjectToTerrain(Terrain terrain) =>
            nodes = terrain.ProjectSegmentToTerrain(Start.Position, End.Position)
                .Select(pos => new Node(pos))
                .ToArray();

        #region DEBUG INFO

        // FOR DEBUGGING
        public Node[] ExploredNodes = Array.Empty<Node>();
        public Node[] OpenNodes = Array.Empty<Node>();

        #endregion
    }
}