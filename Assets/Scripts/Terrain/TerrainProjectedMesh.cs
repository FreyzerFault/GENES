using ExtensionMethods;
using UnityEngine;

namespace Terrain
{
    public class TerrainProjectedMesh : MonoBehaviour
    {
        public float size = 10f;
        public float offset = 0.2f;
        private Mesh _mesh;
        private UnityEngine.Terrain _terrain;


        private void Awake()
        {
            _terrain = FindObjectOfType<UnityEngine.Terrain>();
            _mesh = GetComponent<MeshFilter>().mesh;

            _terrain.CreateMeshWithResolution(size, out var vertices, out var tris);

            _mesh.vertices = vertices;
            _mesh.triangles = tris;

            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
            _mesh.Optimize();
        }

        private void Update()
        {
            transform.LockRotationVertical();
            _terrain.ProjectMeshInTerrain(_mesh, transform, offset);
        }
    }
}