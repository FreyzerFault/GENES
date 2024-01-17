using ExtensionMethods;
using UnityEngine;

namespace Terrain
{
    public class TerrainGeneratedMesh : MonoBehaviour
    {
        public float size;

        public float offset = 0.2f;
        private Mesh _mesh;
        private UnityEngine.Terrain _terrain;

        private void Update()
        {
            _terrain = FindObjectOfType<UnityEngine.Terrain>();
            _mesh = GetComponent<MeshFilter>().mesh;

            var position = transform.position;
            _terrain.GetMeshPatch(out var vertices, out var tris, offset, new Vector2(position.x, position.z), size);

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(tris, 0);

            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
            _mesh.Optimize();
        }
    }
}