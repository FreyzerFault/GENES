using ExtensionMethods;
using UnityEngine;

namespace Terrain
{
    public class TerrainProjectedMesh : MonoBehaviour
    {
        public Vector2 size = Vector2.one * 10;
        public float offset = 0.2f;
        private Mesh _mesh;
        private UnityEngine.Terrain _terrain;

        public bool realTimeUpdate = false;


        private void Awake()
        {
            _terrain = FindObjectOfType<UnityEngine.Terrain>();
            _mesh = GetComponent<MeshFilter>().mesh;

            _mesh.GenerateMeshPlane(_terrain.terrainData.heightmapScale.x / 2, new Vector2(_mesh.bounds.size.x, _mesh.bounds.size.z));
            
            _terrain.ProjectMeshInTerrain(_mesh, transform, offset);
            transform.LockRotationVertical();
        }

        private void Update()
        {
            if (realTimeUpdate)
            {
                _terrain.ProjectMeshInTerrain(_mesh, transform, offset);
                transform.LockRotationVertical();
            }
        }
    }
}