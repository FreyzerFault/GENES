using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.MeshExtensions;
using UnityEngine;

namespace GENES.Utils
{
	public class ProjectedMeshOnTerrain : MonoBehaviour
	{
		public Vector2 size = Vector2.one * 10;
		public float resolution = 1;
		public float offset = 0.2f;

		public bool realTimeUpdate;
		private Mesh _mesh;
		private Terrain _terrain;

		private void Awake()
		{
			_terrain = Terrain.activeTerrain;
			_mesh = GetComponent<MeshFilter>().mesh;

			_mesh = MeshGeneration.GenerateMeshPlane(resolution, size);
			ProjectOnTerrain();
		}

		private void Update()
		{
			if (realTimeUpdate) ProjectOnTerrain();

			transform.LockRotationVertical();
		}

		private void ProjectOnTerrain() => _terrain.ProjectMeshInTerrain(_mesh, transform, offset);
	}
}
