using System;
using Core;
using DavidUtils;
using DavidUtils.ExtensionMethods;
using Map.Rendering;
using Procrain.MapGeneration;
using UnityEngine;

namespace Map
{
    public enum MapState
    {
        Minimap,
        Fullscreen,
        Hidden
    }

    public class MapManager : Singleton<MapManager>
    {
        [SerializeField] private MapState mapState;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject water;
        
        // MAP Rendering settings
        public HeightMap heightMap;
        public TerrainSettingsSo terrainSettings;
        public Gradient heightGradient = new();
        [SerializeField] private float zoomMap = 1;
        [SerializeField] private float zoomMinimap = 1;
        
        
        // PATH FINDING
        public PathFindingGenerator mainPathFindingGenerator;
        
        public event Action<MapState> OnStateChanged;
        public event Action<float> OnZoomMapChanged;

        public MapState MapState
        {
            get => mapState;
            private set
            {
                mapState = value;
                HandleStateChanged(value);
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
                        OnZoomMapChanged?.Invoke(value);
                        break;
                    case MapState.Minimap:
                        zoomMinimap = value;
                        OnZoomMapChanged?.Invoke(value);
                        break;
                    case MapState.Hidden:
                    default:
                        break;
                }
            }
        }

        // TERRAIN CONTEXT
        public static Terrain Terrain => Terrain.activeTerrain;
        public float TerrainWidth => Terrain.terrainData.size.x;
        public float TerrainHeight => Terrain.terrainData.size.z;
        public float WaterHeight => water == null ? 0 : water.transform.position.y;
        
        [SerializeField] private GameObject minimap;
        [SerializeField] private GameObject fullScreenMap;

        [SerializeField] private GameObject MinimapParent => minimap.transform.parent.gameObject;
        [SerializeField] private GameObject FullScreenMapParent => fullScreenMap.transform.parent.gameObject;

        // PLAYER CONTEXT
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

            HandleStateChanged(mapState);
            UpdateMap();
        }

        private void Start()
        {
            GameObject mainPathFindingObj = GameObject.FindWithTag("Map Path Main");
            if (mainPathFindingObj != null)
                mainPathFindingGenerator = mainPathFindingObj.GetComponent<PathFindingGenerator>();
        }
        
        private void HandleStateChanged(MapState state)
        {
            if (!fullScreenMap.activeSelf || !minimap.activeSelf) return; 
            FullScreenMapParent.SetActive(false);     
            MinimapParent.SetActive(false);

            switch (state)
            {
                case MapState.Fullscreen:
                    FullScreenMapParent.SetActive(true);
                    break;
                case MapState.Minimap:
                    MinimapParent.SetActive(true);
                    break;
            }
            
            OnStateChanged?.Invoke(state);
        }

        public void UpdateMap() =>
            heightMap =
                terrainSettings != null
                    ? HeightMapGenerator.CreatePerlinNoiseHeightMap(
                        terrainSettings.NoiseParams,
                        terrainSettings.HeightCurve
                    )
                    : new HeightMap(Terrain);

        public void ZoomInOut(float zoomScale = 1)
        {
            if (minimap == null || fullScreenMap == null) return;
            switch (MapState)
            {
                case MapState.Minimap:
                    minimap.GetComponent<MapRendererUI>().ZoomIn(zoomScale);
                    break;
                case MapState.Fullscreen:
                    fullScreenMap.GetComponent<MapRendererUI>().ZoomIn(zoomScale);
                    break;
            }
        }

        public bool IsLegalPos(Vector2 normPos) => IsLegalPos(Terrain.GetWorldPosition(normPos));

        public bool IsLegalPos(Vector3 pos)
        {
            bool aboveWater = pos.y >= WaterHeight;

            // TODO : Test this en todas las coords
            bool inBounds = Terrain.terrainData.bounds.Contains(pos);

            // If Pathfinding used, check legality in PathFinding rules
            if (mainPathFindingGenerator == null) return aboveWater && inBounds;
            
            bool pathFindingLegalPos = mainPathFindingGenerator.IsLegalPos(pos);
            return aboveWater && inBounds && pathFindingLegalPos;
        }

        public void ToggleMap()
        {
            MapState = MapState switch
            {
                MapState.Minimap => MapState.Fullscreen,
                MapState.Fullscreen => MapState.Minimap,
                _ => MapState.Fullscreen
            };
            
            GameManager.Instance.State = MapState == MapState.Fullscreen
                ? GameManager.GameState.Paused
                : GameManager.GameState.Playing;
        }
    }
}