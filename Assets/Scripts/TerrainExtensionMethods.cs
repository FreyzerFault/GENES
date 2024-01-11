using UnityEngine;

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
}