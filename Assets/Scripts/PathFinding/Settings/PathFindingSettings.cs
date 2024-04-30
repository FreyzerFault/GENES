using System;
using PathFinding.Algorithms;
using PathFinding.Settings.Astar;
using PathFinding.Settings.Astar_Directional;
using PathFinding.Settings.Dijkstra;
using UnityEngine;

namespace PathFinding.Settings
{
	[CreateAssetMenu(fileName = "PathFinding Config", menuName = "PathFinding/PathFinding Configuration", order = 1)]
	public class PathFindingSettings : ScriptableObject
	{
		#region CACHE

		// Cache para evitar recalcular el camino
		public bool useCache = true;

		#endregion

		public event Action OnFineTune;
		public void OnFineTuneTriggerEvent() => OnFineTune?.Invoke();

		#region RESTRICTIONS

		public float maxSlopeAngle = 30f;
		public float minHeight = 20f;
		public bool LegalHeight(float height) => height >= minHeight;
		public bool LegalSlope(float slopeAngle) => slopeAngle <= maxSlopeAngle;

		#endregion

		#region GENERAL PARAMETERS

		// Distancia entre Nodos
		// A + grandes = + rápido pero + impreciso
		public float cellSize = 1f;

		// Maximas iteraciones del algoritmo
		public int maxIterations = 1000;

		#endregion

		#region ALGORITHM

		// DEPENDENCY INJECTION del Algoritmo usado
		[SerializeField] public PathFindingAlgorithmType algorithm;

		public AstarParameters aStarParameters;
		public AstarDirectionalParameters aStarDirectionalParameters;
		public DijkstraParameters dijkstraParameters;

		public PathFindingAlgorithm Algorithm =>
			algorithm switch
			{
				PathFindingAlgorithmType.Astar => Algorithms.Astar.Instance,
				PathFindingAlgorithmType.AstarDirectional => AstarDirectional.Instance,
				PathFindingAlgorithmType.Dijkstra => DijkstraAlg.Instance,
				_ => throw new ArgumentOutOfRangeException()
			};

		public AlgorithmParams Parameters =>
			algorithm switch
			{
				PathFindingAlgorithmType.Astar => aStarParameters,
				PathFindingAlgorithmType.AstarDirectional => aStarDirectionalParameters,
				PathFindingAlgorithmType.Dijkstra => dijkstraParameters,
				_ => throw new ArgumentOutOfRangeException()
			};

		#endregion
	}
}
