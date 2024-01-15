using ExtensionMethods;
using UnityEngine;

namespace Terrain
{
    public class TerrainGrid : MonoBehaviour
    {
        public float offset = 0.2f;
        public int neighbourhood = 1;
        private Mesh _mesh;
        private UnityEngine.Terrain _terrain;

        private void Awake()
        {
            _terrain = FindObjectOfType<UnityEngine.Terrain>();
            _mesh = GetComponent<MeshFilter>().mesh;
        }

        private void Update()
        {
            transform.LockRotationVertical();
            _terrain.ProjectMeshInTerrain(_mesh, transform, offset);
        }
    }
}