using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;

namespace TreesGeneration
{
	public class SpawnerOlivos : MonoBehaviour
	{
		[SerializeField] private GameObject[] olivoPrefabs;

		[SerializeField] private int numFincas = 10;

		private Voronoi voronoi;
		private Bounds bounds;

		[SerializeField] private float minSeparation = 5;
		[SerializeField] private float minDistToBoundary = 5;
		[SerializeField] private float boundaryOffset = 10;

		private void Start()
		{
			bounds = Terrain.activeTerrain.GetBounds();
			voronoi = new Voronoi(numFincas);
		}

		// private void Update()
		// {
		// 	if (Input.anyKeyDown)  voronoi.GenerateVoronoi_OneIteration();
		// }

		// private void InstantiateOlivo(Vector3 position, Quaternion rotation)
		// {
		// Vector3 position = 
		// Instantiate(olivoPrefabs[Random.Range(0, olivoPrefabs.Length)], transform, );
		// }

		#region DEBUG

		private void OnDrawGizmos()
		{
			Vector3 pos = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
			voronoi?.OnDrawGizmos(pos, bounds.size.ToVector2xz(), Color.black);
		}

		#endregion
	}
}
