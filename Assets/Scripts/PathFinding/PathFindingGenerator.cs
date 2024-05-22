using DavidUtils.TerrainExtensions;
using PathFinding.Algorithms;
using PathFinding.Settings;
using Procrain.Core;
using UnityEngine;

namespace PathFinding
{
	public class PathFindingGenerator : PathGenerator
	{
		public PathFindingSettings pathFindingSettings;
		public PathFindingAlgorithm PathFindingAlgorithm => pathFindingSettings.Algorithm;

		[SerializeField] private bool minHeightIsWaterHeight;

		private new void Start()
		{
			if (minHeightIsWaterHeight)
				pathFindingSettings.minHeight = MapManager.Instance.WaterHeight;

			// Redo PathFinding from zero
			PathFindingAlgorithm.CleanCache();

			// Subscribe to PathFindingConfig changes
			pathFindingSettings.OnFineTune += RedoPath_UnvalidateCache;

			base.Start();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			pathFindingSettings.OnFineTune -= RedoPath_UnvalidateCache;
		}

		#region BUILD PATH

		protected override Path BuildPath(Vector3 start, Vector3 end, Vector2? startDir = null, Vector2? endDir = null)
		{
			if (start == end) return Path.EmptyPath;

			var startNode = new Node(start, size: pathFindingSettings.cellSize, direction: startDir);
			var endNode = new Node(end, size: pathFindingSettings.cellSize, direction: endDir);

			return PathFindingAlgorithm.FindPath(startNode, endNode, terrain, bounds, pathFindingSettings);
		}

		#endregion

		#region RESTRICTIONS

		public override bool IsLegal(Vector3 pos)
		{
			bool legalHeight = pathFindingSettings.LegalHeight(pos.y),
				legalSlope = pathFindingSettings.LegalSlope(terrain.GetSlopeAngle(pos)),
				legalPosition = IsInBounds(pos);

			return legalHeight && legalSlope && legalPosition;
		}

		#endregion

		public void RedoPath_UnvalidateCache()
		{
			PathFindingAlgorithm.CleanCache();
			RedoPath();
		}
	}
}
