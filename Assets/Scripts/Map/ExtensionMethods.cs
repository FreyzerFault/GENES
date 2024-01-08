using UnityEngine;

namespace Map
{
    public static class ExtensionMethods
    {
        // =========================== TERRAIN ===========================

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

        // =========================== MARKER ===========================

        // Compara 2 markers, si estan a menos de maxDeltaRadius de distancia, devuelve true
        public static bool DistanceTo(this MapMarkerData marker, MapMarkerData other, float maxDeltaRadius = 0.1f)
        {
            return Vector2.Distance(marker.normalizedPosition, other.normalizedPosition) < maxDeltaRadius;
        }

        public static bool IsAtPoint(this MapMarkerData marker, Vector2 normalizedPos, float maxDeltaRadius = 0.1f)
        {
            return Vector2.Distance(marker.normalizedPosition, normalizedPos) < maxDeltaRadius;
        }

        public static float DistanceTo(this MapMarkerData marker, Vector2 normalizedPos)
        {
            return Vector2.Distance(marker.normalizedPosition, normalizedPos);
        }

        public static float DistanceTo(this MapMarkerData marker, Vector3 globalPos)
        {
            return Vector3.Distance(marker.worldPosition, globalPos);
        }
    }
}