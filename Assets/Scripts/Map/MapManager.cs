using System;
using ExtensionMethods;
using MapGeneration;
using UnityEngine;

namespace Map
{
    public enum MapState
    {
        Minimap,
        Fullscreen,
        Hidden
    }

    [ExecuteAlways]
    public class MapManager : Singleton<MapManager>
    {
        public Gradient heightGradient = new();

        // PATH FINDING
        public PathFindingGenerator mainPathFindingGenerator;

        [SerializeField] private MapState mapState;

        [SerializeField] private GameObject player;

        [SerializeField] private GameObject water;

        [SerializeField] private float zoomMap = 1;

        [SerializeField] private float zoomMinimap = 1;

        // MAP
        public TerrainSettingsSo terrainSettings;

        public HeightMap heightMap;

        public MapState MapState
        {
            get => mapState;
            set
            {
                mapState = value;
                OnStateChanged?.Invoke(value);
            }
        }

        public float Zoom
        {
            get =>
                mapState switch
                {
                    MapState.Minimap => zoomMinimap,
                    MapState.Fullscreen => zoomMap,
                    _ => -1
                };
            set
            {
                value = Mathf.Max(value, 1);
                switch (mapState)
                {
                    case MapState.Fullscreen:
                        zoomMap = value;
                        OnZoomChanged?.Invoke(value);
                        break;
                    case MapState.Minimap:
                        zoomMinimap = value;
                        OnZoomChanged?.Invoke(value);
                        break;
                    case MapState.Hidden:
                    default:
                        break;
                }
            }
        }

        // TERRAIN
        public static Terrain Terrain => Terrain.activeTerrain;

        public float TerrainWidth => Terrain.terrainData.size.x;
        public float TerrainHeight => Terrain.terrainData.size.z;

        public float WaterHeight => water == null ? 0 : water.transform.position.y;

        // PLAYER
        public Vector3 PlayerPosition => player.transform.position;
        public Vector3 PlayerForward => player.transform.forward;

        public Vector2 PlayerNormalizedPosition =>
            Terrain.GetNormalizedPosition(player.transform.position);

        public Vector2 PlayerDistanceToTopRightCorner => Vector2.one - PlayerNormalizedPosition;

        private float PlayerRotationAngle => player.transform.eulerAngles.y;

        public Quaternion PlayerRotationForUI =>
            Quaternion.AngleAxis(90 + PlayerRotationAngle, Vector3.back);

        protected override void Awake()
        {
            base.Awake();

            player = GameObject.FindGameObjectWithTag("Player");
            water = GameObject.FindGameObjectWithTag("Water");

            UpdateMap();
        }

        private void Start()
        {
            var mainPathFindingObj = GameObject.FindWithTag("Map Path Main");
            if (mainPathFindingObj != null)
                mainPathFindingGenerator = mainPathFindingObj.GetComponent<PathFindingGenerator>();
        }

        public event Action<MapState> OnStateChanged;
        public event Action<float> OnZoomChanged;

        public void UpdateMap() =>
            heightMap =
                terrainSettings != null
                    ? HeightMapGenerator.CreatePerlinNoiseHeightMap(
                        terrainSettings.NoiseParams,
                        terrainSettings.HeightCurve
                    )
                    : new HeightMap(Terrain);

        public void ZoomIn(float zoomAmount = 0.1f) => OnZoomChanged?.Invoke(zoomAmount);

        public bool IsLegalPos(Vector2 normPos) => IsLegalPos(Terrain.GetWorldPosition(normPos));

        public bool IsLegalPos(Vector3 pos)
        {
            var aboveWater = pos.y >= WaterHeight;

            // TODO : Test this en todas las coords
            var inBounds = Terrain.terrainData.bounds.Contains(pos);

            // If Pathfinding used, check legality in PathFinding rules
            if (mainPathFindingGenerator != null)
            {
                var pathFindingLegalPos = mainPathFindingGenerator.IsLegalPos(pos);
                return aboveWater && inBounds && pathFindingLegalPos;
            }

            return aboveWater && inBounds;
        }
    }
}