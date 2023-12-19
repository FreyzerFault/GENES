using EditorCools;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public float zoomScale = 2;
    public Gradient heightGradient = new();

    [SerializeField] private RectTransform debugSprite1;
    [SerializeField] private RectTransform debugSprite2;

    [SerializeField] private GameObject player;
    private RawImage _image;
    private RectTransform _playerPointInMap;
    private TerrainData _terrainData;

    private float Width => _image.rectTransform.rect.width;
    private float Height => _image.rectTransform.rect.height;

    private Vector3 MapCeroCero => new(
        _image.rectTransform.position.x - Width / 4,
        _image.rectTransform.position.y - Height / 4,
        0
    );

    private Vector3 LocalPlayerPosInMap => new(
        player.transform.position.x / _terrainData.size.x * Width / 4,
        player.transform.position.z / _terrainData.size.z * Height / 4,
        0
    );

    private void Awake()
    {
        _terrainData ??= FindObjectOfType<Terrain>().terrainData;
        _image ??= GetComponent<RawImage>();
        UpdateMap();
    }

    private void Update()
    {
        UpdatePlayerPoint();
        ZoomToPlayer();
    }


    [Button("Update Map")]
    private void UpdateMap()
    {
        RenderTerrain();
        ZoomToPlayer();
        UpdatePlayerPoint();
    }


    [Button("Update Player Point")]
    private void UpdatePlayerPoint()
    {
        player ??= GameObject.FindGameObjectWithTag("Player");
        _playerPointInMap = GetComponentInChildren<Image>().rectTransform;

        var rotationAngle = 90 + player.transform.eulerAngles.y;
        _playerPointInMap.SetPositionAndRotation(MapCeroCero + LocalPlayerPosInMap,
            Quaternion.AngleAxis(rotationAngle, Vector3.back));
    }

    [Button("Zoom to PLayer")]
    private void ZoomToPlayer()
    {
        _terrainData ??= FindObjectOfType<Terrain>().terrainData;
        _image ??= GetComponent<RawImage>();

        // Centrar el Player en el centro
        var playerToCenter = new Vector3(Width / 2, Height / 2, 0) - LocalPlayerPosInMap;
        _image.rectTransform.anchoredPosition = new Vector3(
            playerToCenter.x, playerToCenter.y, transform.position.z
        );

        debugSprite1.transform.position = _image.rectTransform.position +
                                          new Vector3(_image.rectTransform.anchoredPosition.x,
                                              _image.rectTransform.anchoredPosition.y, 0);
        debugSprite2.transform.position =
            _image.rectTransform.position;

        // Escalar el mapa relativo al centro donde está el Player
        _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

        // var zoomCornerNormalized = (playerInMapLocalPos - MapResZoomed / 2) / MapResZoomed;
        // zoomCornerNormalized.y = 1 - zoomCornerNormalized.y;
        // var zoomRect = new Rect(zoomCornerNormalized, new Vector2(zoom, zoom));
        // _image.uvRect = zoomRect;
    }

    [Button("Render Terrain")]
    private void RenderTerrain()
    {
        var terrainData = _terrainData;
        var width = terrainData.size.x;
        var height = terrainData.size.z;
        var heightMap = terrainData.GetHeights(0, 0, (int)Width, (int)Height);

        // Heightmap to Texture
        var texture = new Texture2D((int)Width, (int)Height);
        MinMaxHeight(heightMap, terrainData.size.y, out var min, out var max);


        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var heightValue = heightMap[y, x];
            var heightNormalized = Mathf.InverseLerp(min, max, heightValue);

            // texture.SetPixel(x, y, new Color(heightNormalized, heightNormalized, heightNormalized, 1));
            texture.SetPixel(x, y, heightGradient.Evaluate(heightNormalized));
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
}