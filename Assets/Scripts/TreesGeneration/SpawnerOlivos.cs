using System.Collections;
using System.Collections.Generic;
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
			if (animated)
			{
				// TODO
				yield return Run_OneIteration(delay);
				drawGrid = false;
			}
			// TODO
		}

		#region PROGRESSIVE GENERATION

		public bool Ended => olivePositions.Count == voronoi.regions.Count;
		public int iterations;

		protected override void Run_OneIteration()
		{
			if (!delaunay.ended)
			{
				delaunay.Run_OnePoint();
			}
			else if (!voronoi.Ended)
			{
				voronoi.Run_OneIteration();
			}
			else if (!Ended)
			{
				PopulateRegion(iterations);
				iterations++;
			}
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
			if (voronoi.regions.Count == 0) GenerateVoronoiRegions();
		}

		// private void Update()
		// {
		// 	if (Input.anyKeyDown)  voronoi.GenerateVoronoi_OneIteration();
		// }

		private void InstantiateOlivo(Vector3 position, Quaternion rotation) => Instantiate(
			olivoPrefabs[Random.Range(0, olivoPrefabs.Length)],
			position,
			rotation,
			transform
		);

#if UNITY_EDITOR

		#region DEBUG

		private Coroutine delaunayCoroutine;

		public void RunAnimation()
		{
			GenerateSeeds();
			delaunayCoroutine = StartCoroutine(voronoi.delaunay.AnimationCoroutine(0));
		}

		public void StopAnimation() => StopCoroutine(delaunayCoroutine);

		private void OnDrawGizmos()
		{
			if (bounds == default)
				bounds = Terrain.activeTerrain.GetBounds();
			var pos = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
			Vector2 size = bounds.size.ToVector2xz();

			voronoi?.OnDrawGizmos(transform.localToWorldMatrix);
		}

		#endregion

#endif
	}
}
