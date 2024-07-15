using System;
using GENES.PathFinding.Settings;
using UnityEngine;

namespace GENES.PathFinding.Algorithms
{
	public class DijkstraAlg : PathFindingAlgorithm
	{
		private static DijkstraAlg _instance;
		public static DijkstraAlg Instance => _instance ??= new DijkstraAlg();

		public override Path FindPath(
			Node start, Node end, Terrain terrain, Bounds bounds, PathFindingSettings settings
		) =>
			throw new NotImplementedException();

		protected virtual float CalculateCost(Node a, Node b, PathFindingSettings pathFindingSettings) =>
			throw new NotImplementedException();
	}
}
