using EditorCools;
using UnityEngine;

namespace Map
{
    public class HeightMap : MonoBehaviour
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private Transform playerInWorld;

        [SerializeField] private float minHeightValue;
        [SerializeField] private float maxHeightValue = 1000;

        private float[,] _heightMap;


        int TerrainRes => terrain.terrainData.heightmapResolution;
        public float TerrainWidth => terrain.terrainData.size.x;
        public float TerrainHeight => terrain.terrainData.size.z;

        // private float TerrainWidth => terrain.terrainData.size.x / 2;
        // private float TerrainHeight => terrain.terrainData.size.y / 2;

        public Vector3 PlayerNormalizedPosition => new(
            playerInWorld.transform.position.x / TerrainWidth,
            playerInWorld.transform.position.z / TerrainHeight,
            0
        );


        // Distancia normalizada del borde al player:
        // (Permite visualizar el mapa sin salir de la zona del terreno)
        public Vector2 PlayerDistanceToBotLeftBorder => new(PlayerNormalizedPosition.x, PlayerNormalizedPosition.y);

        public Vector2 PlayerDistanceToTopRightBorder =>
            new(1 - PlayerNormalizedPosition.x, 1 - PlayerNormalizedPosition.y);

        public Quaternion PlayerRotationForUI =>
            Quaternion.AngleAxis(90 + playerInWorld.transform.eulerAngles.y, Vector3.back);

        private void Awake()
        {
            Initialize();
        }


        private void Initialize()
        {
            terrain ??= FindObjectOfType<Terrain>();
            playerInWorld ??= GameObject.FindGameObjectWithTag("Player")?.transform;
            int terrainRes = terrain.terrainData.heightmapResolution;
            _heightMap = terrain.terrainData.GetHeights(0, 0, terrainRes, terrainRes);

            // Find Min & Max Height of Terrain
            FindMinMaxHeight();
        }


        public Texture2D GetTexture(int texWidth, int texHeight, Gradient heightGradient)
        {
            _heightMap = terrain.terrainData.GetHeights(0, 0, TerrainRes, TerrainRes);
            
            // Heightmap to Texture
            var texture = new Texture2D(TerrainRes, TerrainRes);

            for (var y = 0; y < TerrainRes; y++)
            for (var x = 0; x < TerrainRes; x++)
            {
                var heightValue = _heightMap[y, x];
                var heightNormalized = Mathf.InverseLerp(minHeightValue, maxHeightValue, heightValue);

                // texture.SetPixel(x, y, new Color(heightNormalized, heightNormalized, heightNormalized, 1));
                texture.SetPixel(x, y, heightGradient.Evaluate(heightNormalized));
            }

            texture.Apply();

            // TODO - Set Texture to UI Image in MapRenderer
            // Scale Texture with UV
            // var uvRect = _image.uvRect;
            // uvRect.width = terrainSize.x / texWidth;
            // uvRect.height = terrainSize.z / texHeight;
            // _image.uvRect = uvRect;

            return texture;
        }

        // Max & Min Height in HeightMap float[,]
        private void FindMinMaxHeight()
        {
            maxHeightValue = 0;
            minHeightValue = terrain.terrainData.size.y;
            foreach (var height in _heightMap)
            {
                maxHeightValue = Mathf.Max(height, maxHeightValue);
                minHeightValue = Mathf.Min(height, minHeightValue);
            }
        }

        public Vector3 GetWorldPosition(Vector2 normalizedPosition)
        {
            var x = Mathf.RoundToInt(normalizedPosition.x * _heightMap.GetLength(1));
            var y = Mathf.RoundToInt(normalizedPosition.y * _heightMap.GetLength(0));
            var height = _heightMap[y, x];
            return new Vector3(x, height, y);
        }

        [Button("Update Terrain Map")]
        private void UpdateMapButton()
        {
            Initialize();
        }
    }
}