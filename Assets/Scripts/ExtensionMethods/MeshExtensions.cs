using UnityEngine;

namespace ExtensionMethods
{
    public static class MeshExtensions
    {
        public static void GenerateMeshPlane(this Mesh mesh, float cellSize, Vector2 size)
        {
            var meshCellSize = cellSize;
            Vector2Int sideCellCount = new Vector2Int(Mathf.CeilToInt(size.x / cellSize), Mathf.CeilToInt(size.y / cellSize));
            var sideVerticesCount = sideCellCount + Vector2Int.one;

            var vertices = new Vector3[sideVerticesCount.x * sideVerticesCount.y];
            var triangles = new int[sideCellCount.x * sideCellCount.y * 6];

            for (var y = 0; y < sideVerticesCount.y; y++)
            for (var x = 0; x < sideVerticesCount.x; x++)
            {
                var vertex = new Vector3(
                    x * meshCellSize,
                    0,
                    y * meshCellSize
                );

                // Center Mesh
                vertex -= new Vector3(size.x, 0, size.y) / 2;

                var vertexIndex = x + y * sideVerticesCount.x;

                vertices[vertexIndex] = vertex;

                if (x >= sideCellCount.x || y >= sideCellCount.y) continue;

                // Triangles
                var triangleIndex = (x + y * sideCellCount.x) * 6;
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount.x + 0;
                triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount.x + 1;

                triangles[triangleIndex + 3] = vertexIndex + 0;
                triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount.x + 1;
                triangles[triangleIndex + 5] = vertexIndex + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();
        }
    }
}
