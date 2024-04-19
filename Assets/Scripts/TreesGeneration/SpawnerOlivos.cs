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

		[SerializeField] private float minSeparation = 5;
		[SerializeField] private float minDistToBoundary = 5;
		[SerializeField] private float boundaryOffset = 10;

		private void Start()
		{
			Bounds bounds = Terrain.activeTerrain.GetBounds();
			var min = new Vector2(bounds.min.x, bounds.min.z);
			var max = new Vector2(bounds.max.x, bounds.max.z);
			voronoi = new Voronoi(numFincas, min, max);
		}

		// private void InstantiateOlivo(Vector3 position, Quaternion rotation)
		// {
		// Vector3 position = 
		// Instantiate(olivoPrefabs[Random.Range(0, olivoPrefabs.Length)], transform, );
		// }

		#region DEBUG

		private void OnDrawGizmos() => voronoi?.OnDrawGizmos(Color.black);

		#endregion
	}
}
