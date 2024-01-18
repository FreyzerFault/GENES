using Map.Markers;
using UnityEngine;

namespace Map
{
    public class MapManager : Singleton<MapManager>
    {
        public MarkerManagerSO markerManager;

        public Terrain terrain;
        [SerializeField] private Transform playerInWorld;

        private float[,] _heightMap;
        public TerrainData TerrainData => terrain.terrainData;


        public float TerrainWidth => terrain.terrainData.size.x;
        public float TerrainHeight => terrain.terrainData.size.z;

        public Vector2 PlayerNormalizedPosition => new(
            playerInWorld.transform.position.x / TerrainWidth,
            playerInWorld.transform.position.z / TerrainHeight
        );


        // Distancia normalizada del borde al player:
        // (Permite visualizar el mapa sin salir de la zona del terreno)
        public Vector2 PlayerDistanceToBotLeftBorder => new(PlayerNormalizedPosition.x, PlayerNormalizedPosition.y);

        public Vector2 PlayerDistanceToTopRightBorder =>
            new(1 - PlayerNormalizedPosition.x, 1 - PlayerNormalizedPosition.y);

        public Quaternion PlayerRotationForUI =>
            Quaternion.AngleAxis(90 + playerInWorld.transform.eulerAngles.y, Vector3.back);

        private new void Awake()
        {
            base.Awake();

            terrain ??= FindObjectOfType<Terrain>();
            playerInWorld ??= GameObject.FindGameObjectWithTag("Player")?.transform;

            markerManager.Initialize();
        }
    }
}