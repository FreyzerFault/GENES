using EditorCools;
using UnityEngine;
using UnityEngine.UI;

public class MapRenderer : MonoBehaviour
{
    public float zoomScale = 2;
    public Gradient heightGradient = new();
    [SerializeField] private GameObject player;
    private RawImage _image;
    private RectTransform _playerPointInMap;

    private TerrainData _terrainData;

    private float Width => _image.rectTransform.rect.width;
    private float Height => _image.rectTransform.rect.height;
    private Vector2 Size => new(Width, Height);

    private Vector3 PlayerNormalizedPosition => new(
        player.transform.position.x / _terrainData.size.x,
        player.transform.position.z / _terrainData.size.z,
        0
    );

    private void Awake()
    {
        Initialize();
        RenderTerrain();
    }

    private void Update()
    {
        UpdatePlayerPoint();
        ZoomToPlayer();
    }


    private void Initialize()
    {
        _terrainData ??= FindObjectOfType<Terrain>().terrainData;
        _image ??= GetComponent<RawImage>();
        player ??= GameObject.FindGameObjectWithTag("Player");
        _playerPointInMap = GetComponentInChildren<Image>().rectTransform;
    }


    private void UpdatePlayerPoint()
    {
        var rotationAngle = 90 + player.transform.eulerAngles.y;
        _playerPointInMap.anchoredPosition = PlayerNormalizedPosition * Size;
        _playerPointInMap.rotation = Quaternion.AngleAxis(rotationAngle, Vector3.back);
    }


    private void ZoomToPlayer()
    {
        // Centrar el Player en el centro del minimapa por medio de su pivot
        Vector2 pivot = PlayerNormalizedPosition;

        // Tamaño del minimapa escalado y Normalizado
        var mapSizeScaled = Size / zoomScale;

        // Limitar el minimapa a la zona del terreno
        // Distancia normalizada del borde al player:
        var distanceToBotLeftBorder = new Vector2(PlayerNormalizedPosition.x, PlayerNormalizedPosition.y);
        var distanceToTopRightBorder = new Vector2(1 - PlayerNormalizedPosition.x, 1 - PlayerNormalizedPosition.y);

        // La distancia a los bordes del minimapa no puede ser menor a la mitad del minimapa
        var minDistanceNormalized = mapSizeScaled / 2 / Size;
        if (distanceToBotLeftBorder.x < minDistanceNormalized.x)
            pivot = new Vector2(minDistanceNormalized.x, pivot.y);
        if (distanceToBotLeftBorder.y < minDistanceNormalized.y)
            pivot = new Vector2(pivot.x, minDistanceNormalized.y);
        if (distanceToTopRightBorder.x < minDistanceNormalized.x)
            pivot = new Vector2(1 - minDistanceNormalized.x, pivot.y);
        if (distanceToTopRightBorder.y < minDistanceNormalized.y)
            pivot = new Vector2(pivot.x, 1 - minDistanceNormalized.y);

        // Pivot reajustado
        _image.rectTransform.pivot = pivot;

        // Escalar el mapa relativo al centro donde está el Player
        _image.rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1);

        // La flecha del player se escala al revés para que no se vea afectada por el zoom
        _playerPointInMap.localScale = new Vector3(1 / zoomScale, 1 / zoomScale, 1);
    }


    private void RenderTerrain()
    {
        var terrainSize = _terrainData.size / 2;
        var heightMap = _terrainData.GetHeights(0, 0, (int)terrainSize.x, (int)terrainSize.z);

        // Heightmap to Texture
        var texture = new Texture2D((int)Width, (int)Height);
        MinMaxHeight(heightMap, terrainSize.y, out var min, out var max);

        for (var y = 0; y < terrainSize.x; y++)
        for (var x = 0; x < terrainSize.z; x++)
        {
            var heightValue = heightMap[y, x];
            var heightNormalized = Mathf.InverseLerp(min, max, heightValue);

            // texture.SetPixel(x, y, new Color(heightNormalized, heightNormalized, heightNormalized, 1));
            texture.SetPixel(x, y, heightGradient.Evaluate(heightNormalized));
        }

        texture.Apply();
        _image.texture = texture;
        // _image.SetNativeSize();

        // Scale Texture with UV
        var uvRect = _image.uvRect;
        uvRect.width = terrainSize.x / Width;
        uvRect.height = terrainSize.z / Height;
        _image.uvRect = uvRect;
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

    [Button("Update Map")]
    private void UpdateMapButton()
    {
        Initialize();
        RenderTerrain();
        ZoomToPlayer();
        UpdatePlayerPoint();
    }

    // BUTTONS
    [Button("Update Player Point")]
    private void UpdatePlayerPointButton()
    {
        Initialize();
        UpdatePlayerPoint();
    }

    [Button("Zoom to PLayer")]
    private void ZoomToPlayerButton()
    {
        Initialize();
        ZoomToPlayer();
    }

    [Button("Render Terrain")]
    private void RenderTerrainButton()
    {
        Initialize();
        RenderTerrain();
    }
}