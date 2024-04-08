using System;
using System.Collections;
using Core;
using DavidUtils;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.ThreadingUtils;
using Map.Rendering;
using Procrain.MapGeneration;
using Procrain.Noise;
using Unity.Jobs;
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
        public bool debugTimer = true;

        // WATER
        [SerializeField] private GameObject water;
        public float WaterHeight => water == null ? 0 : water.transform.position.y;

        // TERRAIN CONTEXT
        public static Terrain Terrain => Terrain.activeTerrain;
        public float TerrainWidth => Terrain.terrainData.size.x;
        public float TerrainHeight => Terrain.terrainData.size.z;

        // PLAYER CONTEXT
        [SerializeField] private GameObject player;
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
            BuildMap();

            SubscribeToValuesUpdated();
        }

        private void Start()
        {
            var mainPathFindingObj = GameObject.FindWithTag("Map Path Main");
            if (mainPathFindingObj != null)
                mainPathFindingGenerator = mainPathFindingObj.GetComponent<PathFindingGenerator>();
        }

        private void OnDestroy()
        {
            // Cancel all processes
            StopAllCoroutines();
            if (!mapJobHandle.IsCompleted) mapJobHandle.Complete();

            if (terrainSettings == null) return;
            terrainSettings.ValuesUpdated -= OnValuesUpdated;

            _heightMapThreadSafe.Dispose();
            _heightCurveThreadSafe.Dispose();
        }

        #region TERRAIN SETTINGS

        public TerrainSettingsSo terrainSettings;
        public bool autoUpdate = true;

        protected virtual void OnValidate()
        {
            if (autoUpdate) SubscribeToValuesUpdated();
        }

        private void SubscribeToValuesUpdated()
        {
            if (terrainSettings == null) return;

            terrainSettings.ValuesUpdated -= OnValuesUpdated;
            if (autoUpdate) terrainSettings.ValuesUpdated += OnValuesUpdated;
        }

        private void OnValuesUpdated()
        {
            if (!autoUpdate) return;

            BuildMap();

            // Actualiza la curva de altura para paralelizacion. La normal podria haber cambiado
            if (paralelized) UpdateHeightCurveThreadSafe();
        }

        #endregion

        #region HEIGHT MAP

        private HeightMap heightMap;
        public Gradient heightGradient = new();

        public event Action<IHeightMap> OnMapUpdated;

        private void BuildMap()
        {
            if (paralelized)
            {
                StartCoroutine(BuildMapParallelizedCoroutine());
            }
            else
            {
                var size = terrainSettings.NoiseParams.size;
                DebugTimer.DebugTime(BuildHeightMap, $"Time to build HeightMap {size} x {size}");
            }
        }

        public void BuildHeightMap()
        {
            heightMap =
                terrainSettings != null
                    ? HeightMapGenerator.CreatePerlinNoiseHeightMap(
                        terrainSettings.NoiseParams,
                        terrainSettings.HeightCurve
                    )
                    : new HeightMap(Terrain);
            OnMapUpdated?.Invoke(heightMap);
        }

        #endregion

        #region State

        [SerializeField] private MapState mapState;
        [SerializeField] private GameObject minimap;
        [SerializeField] private GameObject fullScreenMap;

        private GameObject MinimapParent => minimap.transform.parent.gameObject;
        private GameObject FullScreenMapParent => fullScreenMap.transform.parent.gameObject;

        public event Action<MapState> OnStateChanged;

        public MapState MapState
        {
            get => mapState;
            private set
            {
                mapState = value;
                HandleStateChanged(value);
            }
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

        #endregion

        #region Threading

        private HeightMapThreadSafe _heightMapThreadSafe;
        public IHeightMap HeightMap => paralelized ? _heightMapThreadSafe : heightMap;
        public bool paralelized;

        protected JobHandle mapJobHandle;

        // Heigth Curve for Threading (sampled to a Look Up Table)
        private SampledAnimationCurve _heightCurveThreadSafe;
        private readonly int _heightCurveSamples = 100;

        public AnimationCurve HeightCurve
        {
            get => terrainSettings.HeightCurve;
            set
            {
                terrainSettings.HeightCurve = value;
                UpdateHeightCurveThreadSafe();
            }
        }

        private void UpdateHeightCurveThreadSafe()
        {
            if (terrainSettings.HeightCurve == null) return;
            _heightCurveThreadSafe.Sample(HeightCurve, _heightCurveSamples);
        }

        protected virtual IEnumerator BuildMapParallelizedCoroutine()
        {
            yield return BuildHeightMapParallelizedCoroutine();
            OnMapUpdated?.Invoke(_heightMapThreadSafe);
        }

        protected IEnumerator BuildHeightMapParallelizedCoroutine()
        {
            var time = Time.time;

            var sampleSize = terrainSettings.NoiseParams.SampleSize;
            var seed = terrainSettings.NoiseParams.seed;

            // Initialize HeightMapThreadSafe
            _heightMapThreadSafe = new HeightMapThreadSafe(sampleSize, seed);

            // Sample Curve if empty
            if (_heightCurveThreadSafe.IsEmpty) UpdateHeightCurveThreadSafe();

            // End last Job if didn't ended
            if (!mapJobHandle.IsCompleted) mapJobHandle.Complete();

            // Wait for JobHandle to END
            mapJobHandle = new HeightMapGeneratorThreadSafe.PerlinNoiseMapBuilderJob
            {
                noiseParams = terrainSettings.NoiseParams,
                heightMap = _heightMapThreadSafe,
                heightCurve = _heightCurveThreadSafe
            }.Schedule();

            yield return new WaitUntil(() => mapJobHandle.IsCompleted);

            // MAP GENERATED!!!
            mapJobHandle.Complete();
            OnMapUpdated?.Invoke(_heightMapThreadSafe);

            if (debugTimer) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
        }

        #endregion

        #region ZOOM

        [SerializeField] private float zoomMap = 1;
        [SerializeField] private float zoomMinimap = 1;
        public event Action<float> OnZoomMapChanged;

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

        #endregion

        #region PATHFINDING

        public PathFindingGenerator mainPathFindingGenerator;

        public bool IsLegalPos(Vector2 normPos) => IsLegalPos(Terrain.GetWorldPosition(normPos));

        public bool IsLegalPos(Vector3 pos)
        {
            var aboveWater = pos.y >= WaterHeight;

            // TODO : Test this en todas las coords
            var inBounds = Terrain.terrainData.bounds.Contains(pos);

            // If Pathfinding used, check legality in PathFinding rules
            if (mainPathFindingGenerator == null) return aboveWater && inBounds;

            var pathFindingLegalPos = mainPathFindingGenerator.IsLegalPos(pos);
            return aboveWater && inBounds && pathFindingLegalPos;
        }

        #endregion


        public virtual void ResetSeed() => terrainSettings.Seed = PerlinNoise.GenerateRandomSeed();
    }
}