using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using EditorCools;
using PathFinding;
using UnityEngine;

namespace Map
{
    public class MapPath : MonoBehaviour
    {
        [SerializeField] private AstarConfigSO aStarConfig;

        [SerializeField] private LineRenderer directLinePath;
        [SerializeField] private LineRenderer linePathBetweenMarkers;
        [SerializeField] private LineRenderer linePlayerToMarker;

        // WORLD Markers
        [SerializeField] private GameObject marker3DPrefab;
        public MarkerObject[] markerObjects;
        [SerializeField] private CinemachineTargetGroup camTargetGroup;

        [SerializeField] private bool useAstarAlgorithm = true;
        [SerializeField] private bool projectLineToTerrain = true;
        [SerializeField] private bool showStartingLineAtPlayer = true;
        [SerializeField] private bool showDirectPath = true;

        private Node[] _playerPath;

        private Transform playerTransform;

        private MapMarkerManagerSO MarkerManager => MapManager.Instance.markerManager;

        private void Awake()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
            markerObjects ??= Array.Empty<MarkerObject>();
        }

        private void Start()
        {
            AstarAlgorithm.CleanCache();

            // PATH
            InitializePathLineRenderer();

            // First Marker Objects Spawning
            InitializeMarkerObjects();

            // Aerial Camera Targets
            InitializeCamTargets();
        }

        private void Update()
        {
            if (showStartingLineAtPlayer && MarkerManager.Markers.Length > 0)
                UpdatePlayerPathLineRenderer();

            // Update Direct Line Renderer
            if (showDirectPath)
                UpdateDirectPathLineRenderer();
        }


        // ================== LINE RENDERER ==================
        private void InitializePathLineRenderer()
        {
            MarkerManager.OnMarkerAdded.AddListener(_ => UpdatePathLineRenderer());
            MarkerManager.OnMarkerRemoved.AddListener(_ => UpdatePathLineRenderer());
            MarkerManager.OnMarkersClear.AddListener(ClearPathLines);
            UpdatePathLineRenderer();
        }

        private void UpdatePathLineRenderer()
        {
            UpdatePathLineRenderer(MarkerManager.Markers);
        }

        private void UpdatePathLineRenderer(Marker[] markers)
        {
            UpdatePathLineRenderer(MarkerManager.Markers.Select(marker => marker.worldPosition).ToArray());
        }

        private void UpdatePathLineRenderer(Vector3[] markerPositions)
        {
            // Player -> First Marker Line
            if (showStartingLineAtPlayer) UpdatePlayerPathLineRenderer();

            if (markerPositions.Length < 2) return;

            var pathForMarkers = BuildPath(
                markerPositions,
                MapManager.Instance.terrain
            );

            // Proyectar en el Terreno
            if (projectLineToTerrain) pathForMarkers = ProjectPathToTerrain(pathForMarkers);

            // Update Line Renderer
            linePathBetweenMarkers.positionCount = pathForMarkers.Length;
            linePathBetweenMarkers.SetPositions(pathForMarkers);
        }

        private void UpdatePlayerPathLineRenderer()
        {
            if (MarkerManager.Markers.Length == 0) return;

            // PathFinding
            var pathForPlayer = BuildPath(
                new[] { playerTransform.position, MarkerManager.FirstMarker.worldPosition },
                MapManager.Instance.terrain
            );

            // Proyectar en el Terreno
            if (projectLineToTerrain) pathForPlayer = ProjectPathToTerrain(pathForPlayer);

            // Update LineRenderer
            linePlayerToMarker.positionCount = pathForPlayer.Length;
            linePlayerToMarker.SetPositions(pathForPlayer);
        }


        // Direct Path to every marker
        private void UpdateDirectPathLineRenderer()
        {
            var directPath = MarkerManager.Markers.Select(marker => marker.worldPosition)
                .Prepend(playerTransform.position).ToArray();

            if (projectLineToTerrain)
                directPath = ProjectPathToTerrain(directPath);

            directLinePath.positionCount = directPath.Length;
            directLinePath.SetPositions(directPath);
        }

        private void ClearPathLines()
        {
            linePathBetweenMarkers.positionCount = 0;
            linePathBetweenMarkers.SetPositions(Array.Empty<Vector3>());

            linePlayerToMarker.positionCount = 0;
            linePlayerToMarker.SetPositions(Array.Empty<Vector3>());
        }


        // ================== PATH FINDING ==================
        private Vector3[] BuildPath(Vector3[] checkPoints, Terrain terrain)
        {
            var points = Array.Empty<Vector3>();

            if (useAstarAlgorithm) // A* Algorithm
                for (var i = 1; i < checkPoints.Length; i++)
                    points = points.Concat(
                        AstarAlgorithm.GetPathWorldPoints(
                            AstarAlgorithm.FindPath(
                                new Node(checkPoints[i - 1], 0, aStarConfig.cellSize),
                                new Node(checkPoints[i], 0, aStarConfig.cellSize),
                                terrain,
                                aStarConfig
                            )
                        )
                    ).ToArray();
            else
                points = checkPoints;

            return points;
        }


        // ================== TERRAIN PROJECTION ==================
        private Vector3[] ProjectPathToTerrain(Vector3[] path)
        {
            var finalPath = Array.Empty<Vector3>();
            for (var i = 1; i < path.Length; i++)
                finalPath = finalPath.Concat(ProjectSegmentToTerrain(path[i - 1], path[i]).SkipLast(1)).ToArray();
            finalPath = finalPath.Append(path[^1]).ToArray();

            // Offset en altura
            finalPath = OffsetPoints(finalPath, MarkerManager.heightOffset);

            return finalPath;
        }

        // Upsample un segmento proyectandolo en el terreno
        private Vector3[] ProjectSegmentToTerrain(Vector3 a, Vector3 b)
        {
            var terrain = MapManager.Instance.terrain;

            var distance = Vector3.Distance(a, b);
            var sampleLength = terrain.terrainData.heightmapScale.x;

            // Si el segmento es más corto, no hace falta samplearlo
            if (sampleLength > distance) return new[] { a, b };

            var lineSamples = new List<Vector3>();
            var numSamples = Mathf.FloorToInt(distance / sampleLength);
            for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                // Por cada sample, calcular su altura mapeada al terreno
                var samplePos = a + (b - a) * ((float)sampleIndex / numSamples);
                samplePos.y = terrain.SampleHeight(samplePos);

                lineSamples.Add(samplePos);
            }

            return lineSamples.ToArray();
        }

        private static Vector3[] OffsetPoints(Vector3[] points, float offset)
        {
            if (points.Length == 0) return Array.Empty<Vector3>();
            // Map points adding offset to Heigth
            return points.Select(point =>
            {
                point.y += offset;
                return point;
            }).ToArray();
        }

        // ================== WORLD MARKERS ==================

        private void InitializeMarkerObjects()
        {
            MarkerManager.OnMarkerAdded.AddListener(SpawnMarkerInWorld);
            MarkerManager.OnMarkerRemoved.AddListener(DestroyMarkerInWorld);
            MarkerManager.OnMarkersClear.AddListener(ClearMarkersInWorld);
            foreach (var marker in MarkerManager.Markers) SpawnMarkerInWorld(marker);
        }

        private void SpawnMarkerInWorld(Marker marker)
        {
            var pos = marker.worldPosition;

            var parent = GameObject.FindWithTag("Map Path");
            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, parent.transform)
                .GetComponent<MarkerObject>();

            markerObjects = markerObjects.Append(markerObj).ToArray();
        }

        private void DestroyMarkerInWorld(Marker marker)
        {
            var index = markerObjects.ToList().FindIndex(markerObj => markerObj.Id == marker.id);
            var markerObj = markerObjects[index];
            Destroy(markerObj.gameObject);

            var list = markerObjects.ToList();
            list.RemoveAt(index);
            markerObjects = list.ToArray();
        }

        private void ClearMarkersInWorld()
        {
            foreach (var markerObj in markerObjects) Destroy(markerObj.gameObject);

            markerObjects = Array.Empty<MarkerObject>();
        }

        // ================== CAM TARGETS ==================
        private void InitializeCamTargets()
        {
            MarkerManager.OnMarkerAdded.AddListener(_ => UpdateCamTargets());
            MarkerManager.OnMarkerRemoved.AddListener(_ => UpdateCamTargets());
            MarkerManager.OnMarkersClear.AddListener(ClearCamTargets);
            ClearCamTargets();
            UpdateCamTargets();
        }

        private void UpdateCamTargets()
        {
            foreach (var obj in markerObjects) camTargetGroup.AddMember(obj.transform, 1, 1);
        }

        private void ClearCamTargets()
        {
            camTargetGroup.m_Targets = Array.Empty<CinemachineTargetGroup.Target>();
        }

        // =================================== UI ===================================

        [Button("Ejecutar PathFinding")]
        private void RedoPathFinding()
        {
            AstarAlgorithm.CleanCache();
            UpdatePathLineRenderer();
            UpdatePlayerPathLineRenderer();
        }
    }
}