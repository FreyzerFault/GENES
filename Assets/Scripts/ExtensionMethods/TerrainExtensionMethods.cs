using UnityEngine;

namespace ExtensionMethods
{
    public static class TerrainExtensionMethods
    {
        public static Texture2D ToTexture(this TerrainData terrain, int texWidth, int texHeight,
            Gradient heightGradient)
        {
            // Heightmap to Texture
            var texture = new Texture2D(texWidth, texHeight);

            terrain.GetMinMaxHeight(out var minHeight, out var maxHeight);

            for (var y = 0; y < texHeight; y++)
            for (var x = 0; x < texWidth; x++)
            {
                var height = terrain.GetInterpolatedHeight(x / (float)texWidth, y / (float)texHeight);
                var heightNormalized = Mathf.InverseLerp(minHeight, maxHeight, height);

                texture.SetPixel(x, y, heightGradient.Evaluate(heightNormalized));
            }

            texture.Apply();

            return texture;
        }

        // [0,1] => [0, TerrainWidth] & [0, TerrainHeight]
        public static Vector3 GetWorldPosition(this TerrainData terrain, Vector2 normalizedPos)
        {
            return new Vector3(
                normalizedPos.x * terrain.size.x,
                terrain.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y),
                normalizedPos.y * terrain.size.z
            );
        }

        public static Vector2 GetNormalizedPosition(this TerrainData terrain, Vector3 worldPos)
        {
            return new Vector2(
                worldPos.x / terrain.size.x,
                worldPos.z / terrain.size.z
            );
        }

        public static float GetInterpolatedHeight(this TerrainData terrain, Vector2 normalizedPos)
        {
            return terrain.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);
        }

        public static float GetInterpolatedHeight(this TerrainData terrain, Vector3 worldPos)
        {
            return terrain.GetInterpolatedHeight(terrain.GetNormalizedPosition(worldPos));
        }


        // Max & Min Height in HeightMap float[,]
        public static void GetMinMaxHeight(this TerrainData terrain, out float minHeight, out float maxHeight)
        {
            var terrainRes = terrain.heightmapResolution;
            var heightMap = terrain.GetHeights(0, 0, terrainRes, terrainRes);

            minHeight = terrain.heightmapScale.y;
            maxHeight = 0;

            foreach (var height in heightMap)
            {
                minHeight = Mathf.Min(height * terrain.heightmapScale.y, minHeight);
                maxHeight = Mathf.Max(height * terrain.heightmapScale.y, maxHeight);
            }
        }

        public static float GetSlopeAngle(this Terrain terrain, Vector3 worldPos)
        {
            var normalizedPos = terrain.terrainData.GetNormalizedPosition(worldPos);
            return terrain.GetSlopeAngle(normalizedPos);
        }

        public static float GetSlopeAngle(this Terrain terrain, Vector2 normalizedPos)
        {
            // Terrain Normal => Slope Angle
            return Vector3.Angle(
                Vector3.up,
                terrain.terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y)
            );
        }

        // ================== TERRAIN to MESH ==================

        // PROJECT MESH in Terrain
        public static Mesh ProjectMeshInTerrain(this Terrain terrain, Mesh mesh, Transform meshTransform, float offset)
        {
            var vertices = mesh.vertices;

            for (var i = 0; i < vertices.Length; i++)
            {
                var localPos = vertices[i];
                var worldPos = meshTransform.TransformPoint(localPos);
                worldPos.y = terrain.SampleHeight(worldPos) + offset;
                vertices[i] = meshTransform.InverseTransformPoint(worldPos);
            }

            mesh.vertices = vertices;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }


        // Convierte el Terreno a una Mesh con la mayor resolucion
        public static void GetMesh(this Terrain terrain, out Vector3[] vertices,
            out int[] triangles, float heightOffset)
        {
            var heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution,
                terrain.terrainData.heightmapResolution);

            var cellSize = terrain.terrainData.heightmapScale.x;
            var sideCellCount = heightMap.GetLength(0) - 1;
            var sideVerticesCount = heightMap.GetLength(0);

            vertices = new Vector3[sideVerticesCount * sideVerticesCount];
            triangles = new int[sideCellCount * sideCellCount * 6];

            for (var y = 0; y < sideVerticesCount; y++)
            for (var x = 0; x < sideVerticesCount; x++)
            {
                Vector3 vertex = new(x * cellSize,
                    heightMap[y, x] * terrain.terrainData.heightmapScale.y + heightOffset, y * cellSize);

                var vertexIndex = x + y * sideVerticesCount;

                vertices[vertexIndex] = vertex;

                if (x >= sideCellCount || y >= sideCellCount) continue;

                // Triangles
                var triangleIndex = (x + y * sideCellCount) * 6;
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount + 0;
                triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount + 1;

                triangles[triangleIndex + 3] = vertexIndex + 0;
                triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount + 1;
                triangles[triangleIndex + 5] = vertexIndex + 1;
            }
        }

        // Extract Patch from Terrain (Mesh proyectada sobre el terreno)
        public static void CreateMeshPatch(this Terrain terrain, Mesh mesh, Transform meshTransform,
            Vector3 worldCenter, float size, float heightOffset)
        {
            float cellSize = terrain.terrainData.heightmapScale.x / 2;
            mesh.GenerateMeshPlane(cellSize, Vector2.one * size);

            terrain.ProjectMeshInTerrain(mesh, meshTransform, heightOffset);
        }
        
        // Crea el Patch en la posicion central dada
        public static void GetMeshPatch(this Terrain terrain, out Vector3[] vertices,
            out int[] triangles, float heightOffset, Vector2 center, float size)
        {
            // Vertice más cercano al centro para ajustar la malla al terreno
            var normalizedCenter = terrain.terrainData.GetNormalizedPosition(new Vector3(center.x, 0, center.y));
            var nearestTerrainVertex = terrain.GetNearestVertex(normalizedCenter);

            // Vector [centro -> vertice más cercano]
            var displacementToNearestVertex = nearestTerrainVertex - center;

            // Bounding Box del parche del terreno
            Vector2 minBound = center - Vector2.one * size / 2,
                maxBound = center + Vector2.one * size / 2;

            Vector2 normalizedMinBound =
                    terrain.terrainData.GetNormalizedPosition(new Vector3(minBound.x, 0, minBound.y)),
                normalizedMaxBound = terrain.terrainData.GetNormalizedPosition(new Vector3(maxBound.x, 0, maxBound.y));

            normalizedMinBound.x = normalizedMinBound.x < 0.001f ? 0.001f : normalizedMinBound.x;
            normalizedMinBound.y = normalizedMinBound.y < 0.001f ? 0.001f : normalizedMinBound.y;
            normalizedMaxBound.x = normalizedMaxBound.x > 0.999f ? 0.999f : normalizedMaxBound.x;
            normalizedMaxBound.y = normalizedMaxBound.y > 0.999f ? 0.999f : normalizedMaxBound.y;

            // Se genera a partir de la Bounding Box
            terrain.GetMeshPatch(
                out vertices,
                out triangles,
                heightOffset,
                normalizedMinBound,
                normalizedMaxBound,
                displacementToNearestVertex
            );
        }

        // Crea el Patch con la Bounding Box dada
        public static void GetMeshPatch(this Terrain terrain, out Vector3[] vertices,
            out int[] triangles, float heightOffset, Vector2 minBound, Vector2 maxBound, Vector2 displacement)
        {
            // Calculamos los indices del mapa de altura dentro de la Bounding Box
            var heightMapRes = terrain.terrainData.heightmapResolution;

            var baseIndex = heightMapRes * minBound;
            var count = heightMapRes * (maxBound - minBound);

            int xBase = Mathf.FloorToInt(baseIndex.x),
                yBase = Mathf.FloorToInt(baseIndex.y),
                xSize = Mathf.FloorToInt(count.x),
                ySize = Mathf.FloorToInt(count.y);

            // Mapa de Altura dentro de la Bounding Box
            var heightMap = terrain.terrainData.GetHeights(xBase, yBase, xSize, ySize);

            // Dimensiones de la malla
            var cellSize = terrain.terrainData.heightmapScale.x;
            var sideCellCount = new Vector2Int(heightMap.GetLength(1) - 1, heightMap.GetLength(0) - 1);
            var sideVerticesCount = sideCellCount + Vector2Int.one;
            var sideSize = cellSize * new Vector2(sideCellCount.x, sideCellCount.y);

            vertices = new Vector3[sideVerticesCount.x * sideVerticesCount.y];
            triangles = new int[sideCellCount.x * sideCellCount.y * 6];

            for (var y = 0; y < sideVerticesCount.y; y++)
            for (var x = 0; x < sideVerticesCount.x; x++)
            {
                Vector3 vertex = new(
                    x * cellSize,
                    heightMap[y, x] * terrain.terrainData.heightmapScale.y + heightOffset,
                    y * cellSize
                );

                // Center mesh
                // TODO No se centra bien en algunas posiciones
                vertex += new Vector3(displacement.x, 0, displacement.y) - new Vector3(sideSize.x, 0, sideSize.y) / 2 -
                          new Vector3(cellSize / 2, 0, cellSize / 2);

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
        }

        // Vertice más cercano del terreno a la posición dada (0 altura)
        public static Vector2 GetNearestVertex(this Terrain terrain, Vector2 normalizedPos)
        {
            var cornerIndex = Vector2Int.FloorToInt(terrain.terrainData.heightmapResolution * normalizedPos);
            var cellSize = terrain.terrainData.heightmapScale.x;

            return cellSize * new Vector2(cornerIndex.x, cornerIndex.y);
        }

        public static Vector3 GetNearestVertexByWorldPos(this Terrain terrain, Vector3 worldPos)
        {
            return terrain.GetNearestVertex(terrain.terrainData.GetNormalizedPosition(worldPos));
        }
    }
}