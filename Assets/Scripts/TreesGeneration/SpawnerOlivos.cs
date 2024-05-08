using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.Generators;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TreesGeneration
{
	public class SpawnerOlivos : VoronoiGenerator
	{
		[SerializeField] private GameObject[] olivoPrefabs;

		[SerializeField] private int numFincas = 10;

		[SerializeField] private float minSeparation = 5;
		[SerializeField] private float minDistToBoundary = 5;
		[SerializeField] private float boundaryOffset = 10;

		public Dictionary<Polygon, Vector2[]> olivePositions = new();

		protected override IEnumerator RunCoroutine()
		{
			yield return base.RunCoroutine();
			if (animatedDelaunay)
			{
				Run_OneIteration();
				yield return new WaitForSeconds(delay);
			}
			else
			{
				while (olivePositions.Count != voronoi.regions.Count) Run_OneIteration();
			}

			// Spawn in Positions generated
			SpawnAll();
		}

		#region PROGRESSIVE GENERATION

		public bool Ended => olivePositions.Count == voronoi.regions.Count;
		public int iterations;

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended)
				delaunay.Run_OnePoint();
			else if (!voronoi.Ended)
				voronoi.Run_OneIteration();
			else if (!Ended)
				PopulateRegion(iterations++);
		}

		private void PopulateRegion(int regionIndex)
		{
			List<Vector2> olives = new();

			// TODO

			olivePositions.Add(voronoi.regions[regionIndex], olives.ToArray());
		}

		#endregion


		public void SpawnAll()
		{
			if (olivePositions == null || olivePositions.Count == 0) return;

			foreach (KeyValuePair<Polygon, Vector2[]> valuePair in olivePositions)
			{
				Vector2[] localPositions = valuePair.Value;
				foreach (Vector2 localPosition in localPositions)
				{
					Vector3 position = transform.localToWorldMatrix.MultiplyPoint3x4(localPosition.ToVector3xz());
					Instantiate(RandomOlivePrefab, position, Quaternion.identity, transform);
				}
			}
		}

		private GameObject RandomOlivePrefab => olivoPrefabs[Random.Range(0, olivoPrefabs.Length)];


#if UNITY_EDITOR

		#region DEBUG

		private readonly bool drawOlivosPositions = true;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			if (!drawOlivosPositions) return;

			ColorUtility.TryParseHtmlString("#808000ff", out Color color);
			Gizmos.color = color;
			foreach (Vector2[] regionPositions in olivePositions.Select(pair => pair.Value))
			foreach (Vector2 localPos in regionPositions)
			{
				Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(localPos.ToVector3xz());
				Gizmos.DrawSphere(pos, 1f);
			}
		}

		#endregion

#endif
	}
}
