using System;
using System.Linq;
using Cinemachine;
using PathFinding;
using UnityEngine;
#if UNITY_EDITOR
using MyBox;
#endif

namespace Map.Path
{
    public class PathGenerator : MonoBehaviour
    {
        [SerializeField] private PathFindingConfigSO pathFindingConfig;

        [SerializeField] private PathRenderer playerPathRenderer;
        [SerializeField] private PathRenderer markersPathRenderer;
        [SerializeField] private PathRenderer directPathRenderer;

        // WORLD Markers
        [SerializeField] private GameObject marker3DPrefab;
        public MarkerObject[] markerObjects;
        [SerializeField] private CinemachineTargetGroup camTargetGroup;

        [SerializeField] private bool showDirectPath = true;


        private Transform playerTransform;
        private PathFindingAlgorithm PathFinding => pathFindingConfig.Algorithm;
        private MarkerManagerSO MarkerManager => MapManager.Instance.markerManager;


        private void Awake()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
            markerObjects ??= Array.Empty<MarkerObject>();
        }

        private void Start()
        {
            PathFinding.CleanCache();

            // PATH
            InitializePath();

            // First Marker Objects Spawning
            InitializeMarkerObjects();

            // Aerial Camera Targets
            InitializeCamTargets();
        }

        private void Update()
        {
            UpdatePlayerPath();

            // Update Direct Line Renderer
            if (showDirectPath) UpdateDirectPath();
        }


        // ================== LINE RENDERER ==================
        private void InitializePath()
        {
            MarkerManager.OnMarkerAdded.AddListener((marker, index) =>
            {
                // Si es el 1º marcador se actualiza el camino del jugador
                if (index == 0)
                    UpdatePlayerPath();

                UpdateMarkersPath();
            });
            MarkerManager.OnMarkerRemoved.AddListener((marker, index) =>
            {
                // Si es el 1º marcador se actualiza el camino del jugador
                if (index == 0)
                    UpdatePlayerPath();

                UpdateMarkersPath();
            });
            MarkerManager.OnMarkersClear.AddListener(ClearPathLines);

            UpdatePlayerPath();
            UpdateMarkersPath();
            UpdateDirectPath();
        }

        private void UpdateMarkersPath()
        {
            if (MarkerManager.MarkersCount < 2) return;

            markersPathRenderer.Path = BuildPath(MarkerManager.MarkerWorldPositions);
        }

        private void UpdatePlayerPath()
        {
            if (MarkerManager.MarkersCount == 0) return;

            // Player -> 1º Marker
            playerPathRenderer.Path = BuildPath(new[]
            {
                playerTransform.position,
                MarkerManager.FirstMarker.WorldPosition
            });
        }


        // Direct Path to every marker
        private void UpdateDirectPath()
        {
            if (MarkerManager.MarkersCount < 2) return;

            directPathRenderer.Path = new PathFinding.Path(MarkerManager.Markers
                .Select(marker => new Node(marker.WorldPosition))
                .Prepend(new Node(playerTransform.position))
                .ToArray()
            );
        }

        private void ClearPathLines()
        {
            markersPathRenderer.ClearLine();
            playerPathRenderer.ClearLine();
            directPathRenderer.ClearLine();
        }


        // ================== PATH FINDING ==================
        private PathFinding.Path BuildPath(Vector3[] checkPoints)
        {
            if (checkPoints.Length == 0) return global::PathFinding.Path.EmptyPath;

            return PathFinding.FindPathByCheckpoints(
                checkPoints.Select(point => new Node(
                    point,
                    size: pathFindingConfig.cellSize
                )).ToArray(),
                MapManager.Instance.terrain,
                pathFindingConfig
            );
        }

        // ================== WORLD MARKERS ==================

        private void InitializeMarkerObjects()
        {
            foreach (var marker in MarkerManager.Markers) SpawnMarkerInWorld(marker);

            MarkerManager.OnMarkerAdded.AddListener(SpawnMarkerInWorld);
            MarkerManager.OnMarkerRemoved.AddListener(DestroyMarkerInWorld);
            MarkerManager.OnMarkersClear.AddListener(ClearMarkersInWorld);
        }

        private void SpawnMarkerInWorld(Marker marker, int index = -1)
        {
            var pos = marker.WorldPosition;

            var parent = GameObject.FindWithTag("Map Path");
            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, parent.transform)
                .GetComponent<MarkerObject>();

            if (index == -1)
            {
                markerObjects = markerObjects.Append(markerObj).ToArray();
            }
            else
            {
                var list = markerObjects.ToList();
                list.Insert(index, markerObj);
                markerObjects = list.ToArray();
            }
        }

        private void DestroyMarkerInWorld(Marker marker, int index)
        {
            var markerObj = markerObjects[index];
            if (Application.isPlaying)
                Destroy(markerObj.gameObject);
            else
                DestroyImmediate(markerObj.gameObject);

            var list = markerObjects.ToList();
            list.RemoveAt(index);
            markerObjects = list.ToArray();
        }

        private void ClearMarkersInWorld()
        {
            foreach (var markerObj in markerObjects)
                if (Application.isPlaying)
                    Destroy(markerObj.gameObject);
                else
                    DestroyImmediate(markerObj.gameObject);

            markerObjects = Array.Empty<MarkerObject>();
        }

        // ================== CAM TARGETS ==================
        private void InitializeCamTargets()
        {
            ClearCamTargets();
            UpdateCamTargets();

            MarkerManager.OnMarkerAdded.AddListener((_, _) => UpdateCamTargets());
            MarkerManager.OnMarkerRemoved.AddListener((_, _) => UpdateCamTargets());
            MarkerManager.OnMarkersClear.AddListener(ClearCamTargets);
        }

        private void UpdateCamTargets()
        {
            ClearCamTargets();
            foreach (var obj in markerObjects) camTargetGroup.AddMember(obj.transform, 1, 1);

            camTargetGroup.AddMember(playerTransform, 1, 1);
        }

        private void ClearCamTargets()
        {
            camTargetGroup.m_Targets = Array.Empty<CinemachineTargetGroup.Target>();
        }

        // =================================== UI ===================================

#if UNITY_EDITOR
        [ButtonMethod]
        private void RedoPathFinding()
        {
            PathFinding.CleanCache();

            UpdateMarkersPath();
            UpdatePlayerPath();
        }
#endif
    }
}