using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TreesGeneration
{
	public class SpawnerOlivos : MonoBehaviour
	{
		[SerializeField] private GameObject[] olivoPrefabs;

		[SerializeField] private int numFincas = 10;

		public Voronoi voronoi;
		private Bounds bounds;

		[SerializeField] private float minSeparation = 5;
		[SerializeField] private float minDistToBoundary = 5;
		[SerializeField] private float boundaryOffset = 10;

		private void Start()
		{
			bounds = Terrain.activeTerrain.GetBounds();
			voronoi = new Voronoi(numFincas);
		}

		public void Initialize()
		{
			voronoi = new Voronoi(numFincas);
		}
		
		public void GenerateSeeds()
		{
			Initialize();
			// voronoi.GenerateSeeds(numFincas);
			voronoi.seeds = GeometryUtils.GenerateRandomSeeds_WaveDistribution(numFincas);
		}

		public void GenerateVoronoiRegions()
		{
			voronoi.GenerateDelaunay();
			voronoi.GenerateVoronoi();
		}
		
		public void SpawnAll() {
			if (voronoi.regions.Length == 0) GenerateVoronoiRegions();
			
		}

		// private void Update()
		// {
		// 	if (Input.anyKeyDown)  voronoi.GenerateVoronoi_OneIteration();
		// }

		private void InstantiateOlivo(Vector3 position, Quaternion rotation)
		{
			Instantiate(olivoPrefabs[Random.Range(0, olivoPrefabs.Length)], position, rotation, transform);
		}

		#region DEBUG

		public bool showDelaunayTriangulation = true;
		public bool projectOnTerrain = true;
		
		Coroutine delaunayCoroutine;
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
			Vector3 pos = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
			Vector2 size = bounds.size.ToVector2xz();
			
			voronoi?.OnDrawGizmos(pos, size, Color.black);
			
			// DELAUNAY
			if (showDelaunayTriangulation)
				voronoi?.delaunay.OnDrawGizmos(pos, size, projectOnTerrain);
		}

		#endregion
	}
}
