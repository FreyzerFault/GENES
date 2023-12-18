using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField] private Vector2 mapRes = new(512, 512);
    private RawImage _image;
    private Terrain _terrain;

    private void Awake()
    {
        _terrain = FindObjectOfType<Terrain>();
        _image = GetComponent<RawImage>();

        RenderTerrain();
    }

    private void RenderTerrain()
    {
        var terrainData = _terrain.terrainData;
        var width = terrainData.size.x;
        var height = terrainData.size.z;
        var heightMap = terrainData.GetHeights(0, 0, (int)mapRes.x, (int)mapRes.y);

        // Heightmap to Texture
        var texture = new Texture2D((int)mapRes.x, (int)mapRes.y);
        MinMaxHeight(heightMap, terrainData.size.y, out var min, out var max);

        for (var y = 0; y < mapRes.y; y++)
        for (var x = 0; x < mapRes.x; x++)
        {
            var heightValue = heightMap[x, y];
            var heightNormalized = Mathf.InverseLerp(min, max, heightValue);

            // texture.SetPixel(x, y, new Color(heightNormalized, heightNormalized, heightNormalized, 1));
            if (heightNormalized > 0.5)
                texture.SetPixel(x, y, Color.Lerp(new Color(73, 36, 36), Color.black, heightNormalized));
            else if (heightNormalized > 0.2)
                texture.SetPixel(x, y, Color.Lerp(Color.green, new Color(73, 36, 36), heightNormalized));
            else if (heightNormalized > 0.1)
                texture.SetPixel(x, y, Color.Lerp(Color.blue, Color.green, heightNormalized));
            else
                texture.SetPixel(x, y, Color.Lerp(Color.cyan, Color.blue, heightNormalized));
        }

        texture.Apply();
        _image.texture = texture;
        _image.SetNativeSize();
    }

    // Max & Min Height in HeightMap float[,]
    private void MinMaxHeight(float[,] heightMap, float terrainMaxHeight, out float min, out float max)
    {
        max = 0;
        min = terrainMaxHeight;
        foreach (var height in heightMap)
        {
            max = Mathf.Max(height, max);
            min = Mathf.Min(height, min);
        }
    }

    // Max & Min Components of Color
    private void MinMaxColorComponents(Color[] colorData, out Color min, out Color max)
    {
        max = new Color(0, 0, 0, 0);
        min = new Color(255, 255, 255, 255);
        foreach (var color in colorData)
        {
            max.r = Mathf.Max(color.r, max.r);
            max.g = Mathf.Max(color.g, max.g);
            max.b = Mathf.Max(color.b, max.b);
            max.a = Mathf.Max(color.a, max.a);
            min.r = Mathf.Min(color.r, min.r);
            min.g = Mathf.Min(color.g, min.g);
            min.b = Mathf.Min(color.b, min.b);
            min.a = Mathf.Min(color.a, min.a);
        }
    }
}