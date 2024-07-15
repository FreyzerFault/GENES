using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace GENES.PathFinding
{
	[Serializable]
	public class Path
	{
		public static Path EmptyPath = new(Array.Empty<Node>());

		[SerializeField] public Node[] nodes;
		public int NodeCount => nodes.Length;
		public bool IsEmpty => nodes.Length == 0;

		public Node Start => nodes.Length > 0 ? nodes[0] : null;
		public Node End => nodes.Length > 0 ? nodes[^1] : null;

		public Path(Node[] nodes) => this.nodes = nodes;
		public Path(Vector3[] points) => nodes = points.Select(point => new Node(point)).ToArray();
		public Path(Node start, Node end) => nodes = ExtractPath(start, end);

		public bool IsIllegal => !nodes[0].Legal || !nodes[^1].Legal;

		private static Node[] ExtractPath(Node start, Node end)
		{
			if (!start.Legal || !end.Legal) return new[] { start, end };

			// From end to start
			var path = new List<Node> { end };

			Node currentNode = end.Parent;
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
			for (var i = 1; i < nodes.Length; i++) length += Vector3.Distance(nodes[i - 1].position, nodes[i].position);

			return length;
		}

		public Vector3[] GetPathWorldPoints() => nodes.Select(node => node.position).ToArray();

		public Vector2[] GetPathNormalizedPoints(Terrain terrain) =>
			nodes.Select(node => terrain.GetNormalizedPosition(node.position)).ToArray();

		public void ProjectToTerrain(Terrain terrain) =>
			nodes = terrain
				.ProjectSegmentToTerrain(Start.position, End.position)
				.Select(pos => new Node(pos))
				.ToArray();

		public static Path operator +(Path a, Path b)
		{
			if (a.IsEmpty) return b;
			if (b.IsEmpty) return a;

			b.Start.Parent = a.End.Equals(b.Start) ? a.End.Parent : a.End;

			return new Path(a.Start, b.End);
		}

		// Convierte una lista de Paths en un solo Path
		public static Path FromPathList(IEnumerable<Path> paths)
			=> paths.Aggregate(EmptyPath, (current, path) => current + path);

		#region DEBUG

		// FOR DEBUGGING
		public Node[] ExploredNodes = Array.Empty<Node>();
		public Node[] OpenNodes = Array.Empty<Node>();

		#endregion
	}
}
