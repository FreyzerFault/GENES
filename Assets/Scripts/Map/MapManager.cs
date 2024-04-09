using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using DavidUtils;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.ThreadingUtils;
using Map.Rendering;
using Procrain.Geometry;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Mesh;
using Procrain.MapGeneration.Texture;
using Procrain.MapGeneration.TIN;
using Procrain.Noise;
using Unity.Collections;
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
        [SerializeField]
        private GameObject water;
        public float WaterHeight => water == null ? 0 : water.transform.position.y;

        // TERRAIN CONTEXT
        public static Terrain Terrain => Terrain.activeTerrain;
        public float TerrainWidth => Terrain.terrainData.size.x;
        public float TerrainHeight => Terrain.terrainData.size.z;

        // PLAYER CONTEXT
        [SerializeField]
        private GameObject player;
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
            if (!_heightMapJobHandle.IsCompleted)
                _heightMapJobHandle.Complete();

            if (terrainSettings == null)
                return;
            terrainSettings.ValuesUpdated -= OnValuesUpdated;

            _heightMapJobHandle.Complete();
            _textureJobHandle.Complete();

            _heightMapThreadSafe.Dispose();
            _heightCurveThreadSafe.Dispose();
            _textureDataThreadSafe.Dispose();
            MeshData_ThreadSafe.Dispose();
        }

        #region TERRAIN SETTINGS

        public TerrainSettingsSo terrainSettings;
        public bool autoUpdate = true;

        protected virtual void OnValidate()
        {
            if (!autoUpdate)
                return;
            SubscribeToValuesUpdated();

            if (paralelized)
                SampleGradient();
        }

        public void SubscribeToValuesUpdated()
        {
            if (terrainSettings == null)
                return;
            terrainSettings.ValuesUpdated -= OnValuesUpdated;

            if (autoUpdate)
                terrainSettings.ValuesUpdated += OnValuesUpdated;
        }

        public void OnValuesUpdated()
        {
            if (!autoUpdate)
                return;

            // Actualiza la curva de altura para paralelizacion. La normal podria haber cambiado
            if (paralelized)
                UpdateHeightCurveThreadSafe();

            BuildMap();
        }

        public virtual void ResetSeed() => terrainSettings.Seed = PerlinNoise.GenerateRandomSeed();

        #endregion

        #region MAP BUILDER

        public event Action<IHeightMap> OnMapUpdated;
        public event Action<Texture2D> OnTextureUpdated;
        public event Action<int, IMeshData> OnMeshUpdated;

        public void BuildMap()
        {
            if (paralelized)
                StartCoroutine(BuildMapParallelizedCoroutine());
            else
                BuildMapSequential();
        }

        public void BuildMapSequential()
        {
            BuildHeightMap_Sequential();
            BuildTexture2D_Sequential();
            BuildMeshData_Sequential(terrainSettings.LOD);
        }

        private IEnumerator BuildMapParallelizedCoroutine()
        {
            yield return BuildHeightMap_ParallelizedCoroutine();
            yield return BuildTexture2D_ParallelizedCoroutine();
            yield return BuildMeshData_ParallelizedCoroutine();
        }

        #region HEIGHT MAP

        private HeightMap _heightMap;
        public int MapSampleSize => terrainSettings.NoiseParams.SampleSize;
        private int MapSize => terrainSettings.NoiseParams.size;

        private void BuildHeightMap()
        {
            if (paralelized)
                StartCoroutine(BuildHeightMap_ParallelizedCoroutine());
            else
                DebugTimer.DebugTime(
                    BuildHeightMap_Sequential,
                    $"Time to build HeightMap {MapSampleSize} x {MapSampleSize}"
                );
        }

        private void BuildHeightMap_Sequential()
        {
            _heightMap =
                terrainSettings != null
                    ? HeightMapGenerator.CreatePerlinNoiseHeightMap(
                        terrainSettings.NoiseParams,
                        terrainSettings.HeightCurve
                    )
                    : new HeightMap(Terrain);
            OnMapUpdated?.Invoke(_heightMap);
        }

        #endregion

        #region TEXTURE

        public Gradient heightGradient = new();
        public Color32[] textureData;
        public Texture2D texture;

        private void BuildTexture2D()
        {
            if (paralelized)
                StartCoroutine(BuildTexture2D_ParallelizedCoroutine());
            else
                DebugTimer.DebugTime(
                    BuildTexture2D_Sequential,
                    $"Time to build Texture {MapSize} x {MapSize}"
                );
        }

        public void BuildTexture2D_Sequential()
        {
            textureData = TextureGenerator.BuildTextureData32(_heightMap, heightGradient);
            texture = TextureGenerator.BuildTexture2D(textureData, MapSize, MapSize);
            OnTextureUpdated?.Invoke(texture);
        }

        // Usa una resolucion distinta
        public void BuildTexture2D_Sequential(Vector2Int resolution)
        {
            textureData = TextureGenerator.BuildTextureData(
                terrainSettings.NoiseParams,
                heightGradient,
                resolution
            );
            texture = TextureGenerator.BuildTexture2D(textureData, resolution.x, resolution.y);
            OnTextureUpdated?.Invoke(texture);
        }

        #endregion

        #region MESH

        private readonly Dictionary<int, IMeshData> meshDataByLoD = new();
        private IMeshData MeshData =>
            paralelized ? MeshData_ThreadSafe : meshDataByLoD[terrainSettings.LOD];

        // Query Mesh by LoD. If not built, build it.
        // If paralellized, return null. So caller may wait for it to get built.
        public IMeshData GetMeshData(int lod = -1)
        {
            if (lod == -1)
                lod = terrainSettings.LOD;
            if (paralelized)
            {
                if (_meshDataByLoD_ThreadSafe.TryGetValue(lod, out var meshData))
                    return meshData;
                StartCoroutine(BuildMeshData_ParallelizedCoroutine(lod));
                return null;
            }
            else
            {
                if (meshDataByLoD.TryGetValue(lod, out var meshData))
                    return meshData;
                BuildMeshData(lod);
                return meshDataByLoD[lod];
            }
        }

        public void BuildMeshData(int lod = -1)
        {
            if (lod == -1)
                lod = terrainSettings.LOD;
            if (paralelized)
                StartCoroutine(BuildMeshData_ParallelizedCoroutine(lod));
            else
                DebugTimer.DebugTime(
                    () => BuildMeshData_Sequential(lod),
                    $"Time to build MeshData {MapSampleSize} x {MapSampleSize}"
                );
        }

        private void BuildMeshData_Sequential(int lod)
        {
            var meshData = MeshGenerator.BuildMeshData(
                _heightMap,
                lod,
                terrainSettings.HeightMultiplier
            );
            meshDataByLoD[lod] = meshData;
            OnMeshUpdated?.Invoke(lod, meshData);
        }

        #endregion

        #region TIN MESH

        // Generar Malla del TIN
        public Tin BuildTin(float errorTolerance, int maxIterations)
        {
            meshDataByLoD[0] = TinGenerator.BuildTinMeshData(
                out var tin,
                _heightMap,
                errorTolerance,
                terrainSettings.HeightMultiplier,
                maxIterations
            );
            return tin;
        }

        #endregion

        #region THREADING

        public bool paralelized;

        #region HEIGHT MAP THREADING

        private HeightMapThreadSafe _heightMapThreadSafe;
        public IHeightMap HeightMap => paralelized ? _heightMapThreadSafe : _heightMap;

        private JobHandle _heightMapJobHandle;

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

        public void UpdateHeightCurveThreadSafe()
        {
            if (terrainSettings.HeightCurve == null)
                return;
            _heightCurveThreadSafe.Sample(HeightCurve, _heightCurveSamples);
        }

        protected IEnumerator BuildHeightMap_ParallelizedCoroutine()
        {
            var time = Time.time;

            var sampleSize = terrainSettings.NoiseParams.SampleSize;
            var seed = terrainSettings.NoiseParams.seed;

            // Initialize HeightMapThreadSafe
            _heightMapThreadSafe = new HeightMapThreadSafe(sampleSize, seed);

            // Sample Curve if empty
            if (_heightCurveThreadSafe.IsEmpty)
                UpdateHeightCurveThreadSafe();

            // If last Job didn't end, wait for it
            if (!_heightMapJobHandle.IsCompleted)
                _heightMapJobHandle.Complete();

            // Wait for JobHandle to END
            _heightMapJobHandle = new HeightMapGeneratorThreadSafe.PerlinNoiseMapBuilderJob
            {
                noiseParams = terrainSettings.NoiseParams,
                heightMap = _heightMapThreadSafe,
                heightCurve = _heightCurveThreadSafe
            }.Schedule();

            yield return new WaitUntil(() => _heightMapJobHandle.IsCompleted);

            // MAP GENERATED!!!
            _heightMapJobHandle.Complete();
            OnMapUpdated?.Invoke(_heightMapThreadSafe);

            if (debugTimer)
                Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
        }

        #endregion

        #region TEXTURE THREADING

        private NativeArray<Color32> _textureDataThreadSafe;
        private GradientThreadSafe _gradientThreadSafe;
        private JobHandle _textureJobHandle;

        protected IEnumerable<Color32> TextureData =>
            paralelized ? _textureDataThreadSafe : textureData;

        private void SampleGradient()
        {
            if (heightGradient == null)
                return;
            _gradientThreadSafe.SetGradient(heightGradient);
        }

        private void InitializeTextureDataThreadSafe()
        {
            // Inicializamos la Textura o la reinicializamos si cambia su tamaño
            if (texture == null || texture.width != _heightMapThreadSafe.Size)
            {
                var size = _heightMapThreadSafe.Size;
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            }

            _textureDataThreadSafe = texture.GetRawTextureData<Color32>();
        }

        protected IEnumerator BuildTexture2D_ParallelizedCoroutine()
        {
            var time = Time.time;

            // Sample Gradient if empty
            if (_gradientThreadSafe.IsEmpty)
                SampleGradient();

            // Get TextureData reference from Texture2D to modify it
            InitializeTextureDataThreadSafe();

            // If last Job didn't end, wait for it
            if (!_textureJobHandle.IsCompleted)
                _textureJobHandle.Complete();

            // Wait for JobHandle to END
            _textureJobHandle = new TextureGeneratorThreadSafe.MapToTextureJob
            {
                heightMap = _heightMapThreadSafe.map,
                textureData = _textureDataThreadSafe,
                gradient = _gradientThreadSafe
            }.Schedule();

            yield return new WaitUntil(() => _textureJobHandle.IsCompleted);

            _textureJobHandle.Complete();

            OnTextureUpdated?.Invoke(texture);

            if (debugTimer)
                Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar la textura");
        }

        #endregion

        #region MESH THREADING

        private readonly Dictionary<int, MeshData_ThreadSafe> _meshDataByLoD_ThreadSafe = new();
        private MeshData_ThreadSafe MeshData_ThreadSafe
        {
            get => _meshDataByLoD_ThreadSafe[terrainSettings.LOD];
            set => _meshDataByLoD_ThreadSafe[terrainSettings.LOD] = value;
        }

        private void InitializeMeshDataThreadSafe(int lod)
        {
            // If no Mesh with this LoD, or Mesh Size changed, reinitialize MeshData
            if (
                !_meshDataByLoD_ThreadSafe.TryGetValue(lod, out var meshData)
                || meshData.IsEmpty
                || meshData.width != MapSampleSize
            )
                MeshData_ThreadSafe = new MeshData_ThreadSafe(MapSize, MapSize);
            else
                MeshData_ThreadSafe.Reset();
        }

        private IEnumerator BuildMeshData_ParallelizedCoroutine(int lod = -1)
        {
            var time = Time.time;

            InitializeMeshDataThreadSafe(lod);

            var meshData = _meshDataByLoD_ThreadSafe[lod];

            var meshJob = new MeshGeneratorThreadSafe.BuildMeshDataJob
            {
                meshData = meshData,
                heightMap = _heightMapThreadSafe,
                lod = lod,
                heightMultiplier = terrainSettings.HeightMultiplier
            }.Schedule();

            yield return new WaitWhile(() => !meshJob.IsCompleted);

            meshJob.Complete();

            OnMeshUpdated?.Invoke(lod, meshData);

            if (debugTimer)
                Debug.Log(
                    $"{(Time.time - time) * 1000:F1} ms para generar la Malla {MapSampleSize} x {MapSampleSize}, LoD {lod}"
                );
        }

        #endregion

        #endregion

        #endregion

        #region STATE

        [SerializeField]
        private MapState mapState;

        [SerializeField]
        private GameObject minimap;

        [SerializeField]
        private GameObject fullScreenMap;

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
            if (!fullScreenMap.activeSelf || !minimap.activeSelf)
                return;
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

            GameManager.Instance.State =
                MapState == MapState.Fullscreen
                    ? GameManager.GameState.Paused
                    : GameManager.GameState.Playing;
        }

        #endregion

        #region ZOOM

        [SerializeField]
        private float zoomMap = 1;

        [SerializeField]
        private float zoomMinimap = 1;
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
            if (minimap == null || fullScreenMap == null)
                return;
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
            if (mainPathFindingGenerator == null)
                return aboveWater && inBounds;

            var pathFindingLegalPos = mainPathFindingGenerator.IsLegalPos(pos);
            return aboveWater && inBounds && pathFindingLegalPos;
        }

        #endregion
    }
}
