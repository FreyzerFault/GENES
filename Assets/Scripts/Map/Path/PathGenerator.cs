using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using ExtensionMethods;
using JetBrains.Annotations;
using Map.Markers;
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

        // Player -> 1º Marker Path
        [SerializeField] private PathRenderer playerPathRenderer;

        // Direct Path
        [SerializeField] private PathRenderer directPathRenderer;

        // Paths between markers
        [SerializeField] private PathRenderer markerPathRendererPrefab;
        [SerializeField] private PathRenderer[] markersPathRenderers = Array.Empty<PathRenderer>();
        [SerializeField] private Transform markersPathParent;

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

            // Delete previous markers path
            foreach (var obj in markersPathRenderers)
                if (Application.isPlaying)
                    Destroy(obj.gameObject);
                else
                    DestroyImmediate(obj.gameObject);
            markersPathRenderers = Array.Empty<PathRenderer>();

            for (var i = 0; i < MarkerManager.MarkersCount - 1; i++)
            {
                var marker = MarkerManager.Markers[i];

                // Create PathRenderer
                var pathRenderer = Instantiate(markerPathRendererPrefab, markersPathParent);
                markersPathRenderers = markersPathRenderers.Append(pathRenderer).ToArray();

                // PATH
                pathRenderer.Path = BuildPath(
                    new[] { marker.WorldPosition, MarkerManager.FindNextMarker(marker).WorldPosition },
                    out var exploredNodes,
                    out var openNodes
                );

                // All NODES
                pathRenderer.exploredNodes = exploredNodes;
                pathRenderer.openNodes = openNodes;

                // COLOR
                pathRenderer.color =
                    i == 0
                        ? ColorExtensions.RandomColorSaturated()
                        : markersPathRenderers[i - 1].color.RotateHue(0.1f);
            }
        }

        private void UpdatePlayerPath()
        {
            if (MarkerManager.MarkersCount == 0) return;

            // Player -> 1º Marker
            playerPathRenderer.Path = BuildPath(
                new[]
                {
                    playerTransform.position,
                    MarkerManager.Markers.First(marker => marker.IsNext).WorldPosition
                },
                out var exploredNodes,
                out var openNodes);

            playerPathRenderer.exploredNodes = exploredNodes;
            playerPathRenderer.openNodes = openNodes;
        }


        // Direct Path to every marker
        private void UpdateDirectPath()
        {
            if (MarkerManager.MarkersCount < 1) return;

            directPathRenderer.Path = new PathFinding.Path(MarkerManager.Markers
                .Where(marker => marker.State != MarkerState.Checked)
                .Select(marker => new Node(marker.WorldPosition))
                .Prepend(new Node(playerTransform.position))
                .ToArray()
            );
        }

        private void ClearPathLines()
        {
            foreach (Transform obj in markersPathParent)
                obj.GetComponent<PathRenderer>().ClearLine();
            playerPathRenderer.ClearLine();
            directPathRenderer.ClearLine();
        }


        // ================== PATH FINDING ==================
        private PathFinding.Path BuildPath(Vector3[] checkPoints, [CanBeNull] out List<Node> exploredNodes,
            [CanBeNull] out List<Node> openNodes)
        {
            exploredNodes = new List<Node>();
            openNodes = new List<Node>();

            if (checkPoints.Length == 0) return global::PathFinding.Path.EmptyPath;

            return PathFinding.FindPathByCheckpoints(
                checkPoints.Select(point => new Node(
                    point,
                    size: pathFindingConfig.cellSize
                )).ToArray(),
                MapManager.Instance.terrain,
                pathFindingConfig,
                out exploredNodes,
                out openNodes
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
            markerObj.Data = marker;
            markerObj.onPlayerPickUp.AddListener(markerPicked =>
            {
                var nextMarker = MarkerManager.FindNextMarker(markerPicked);
                if (nextMarker != null)
                    nextMarker.State = MarkerState.Next;
            });

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