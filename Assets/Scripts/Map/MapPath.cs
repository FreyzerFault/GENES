using System;
using System.Collections.Generic;
using System.Linq;
using EditorCools;
using JetBrains.Annotations;
using PathFinding;
using UnityEngine;
using UnityEngine.Serialization;

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
        public MapMarkerObject[] markerObjects;

        [SerializeField] private bool useAstarAlgorithm = true;
        [SerializeField] private bool projectLineToTerrain = true;
        [SerializeField] private bool showStartingLineAtPlayer = true;
        [SerializeField] private bool showDirectPath = true;

        private Transform playerTransform;
        private MapMarkerManagerSO MarkerManager => MapManager.Instance.markerManager;

        private Node[] playerPath;

        private void Awake()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
            markerObjects ??= Array.Empty<MapMarkerObject>();
        }

        private void Start()
        {
            MarkerManager.OnMarkerAdded.AddListener(_ => UpdatePath());
            MarkerManager.OnMarkerAdded.AddListener(SpawnMarkerInWorld);
            MarkerManager.OnMarkerRemoved.AddListener(_ => UpdatePath());
            MarkerManager.OnMarkerRemoved.AddListener(DestroyMarkerInWorld);
            MarkerManager.OnMarkersClear.AddListener(ClearLine);
            MarkerManager.OnMarkersClear.AddListener(ClearMarkersInWorld);
            
            AstarAlgorithm.CleanCache();

            UpdatePath();

            // First Markers Spawning
            InitializeMarkerObjects();
        }

        private void Update()
        {
            if (showStartingLineAtPlayer && MarkerManager.Markers.Length > 0)
                UpdatePlayerPath();
            
            
            // Update Direct Line Renderer
            if (showDirectPath)
                UpdateDirectPath();
        }


        // ================== LINE RENDERER ==================

        private void UpdatePath()
        {
            UpdatePath(MarkerManager.Markers);
        }

        private void UpdatePath(MapMarkerData[] markers)
        {
            UpdatePath(MarkerManager.Markers.Select(marker => marker.worldPosition).ToArray());
        }

        [Button("Ejecutar PathFinding")]
        private void UpdatePath(Vector3[] markerPositions)
        {
            // Player -> First Marker Line
            if (showStartingLineAtPlayer) UpdatePlayerPath();
            
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
        
        private void UpdatePlayerPath()
        {
            if (MarkerManager.Markers.Length == 0) return;
            
            // PathFinding
            var pathForPlayer = BuildPath(
                new [] { playerTransform.position, MarkerManager.FirstMarker.worldPosition },
                MapManager.Instance.terrain
            );
            
            // Proyectar en el Terreno
            if (projectLineToTerrain) pathForPlayer = ProjectPathToTerrain(pathForPlayer);
            
            // Update LineRenderer
            linePlayerToMarker.positionCount = pathForPlayer.Length;
            linePlayerToMarker.SetPositions(pathForPlayer);
        }
        
        
        // Direct Path to every marker
        private void UpdateDirectPath()
        {
            Vector3[] directPath = MarkerManager.Markers.Select(marker => marker.worldPosition)
                .Prepend(playerTransform.position).ToArray();
            
            if (projectLineToTerrain)
                directPath = ProjectPathToTerrain(directPath);
            
            directLinePath.positionCount = directPath.Length;
            directLinePath.SetPositions(directPath);
        }

        // ================== PATH FINDING ==================
        private Vector3[] BuildPath(Vector3[] checkPoints, Terrain terrain)
        {
            Vector3[] points = Array.Empty<Vector3>();

            if (useAstarAlgorithm) // A* Algorithm
                for (int i = 1; i < checkPoints.Length; i++)
                {
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
                }
            else
                points = checkPoints;
            
            return points;
        }


        private void ClearLine()
        {
            linePathBetweenMarkers.positionCount = 0;
            linePathBetweenMarkers.SetPositions(Array.Empty<Vector3>());
            
            linePlayerToMarker.positionCount = 0;
            linePlayerToMarker.SetPositions(Array.Empty<Vector3>());
        }
        
        private Vector3[] ProjectPathToTerrain(Vector3[] path)
        {
            Vector3[] finalPath = Array.Empty<Vector3>();
            for (int i = 1; i < path.Length; i++) 
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
            if (sampleLength > distance) return new[] {a, b};
            
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
            foreach (var marker in MarkerManager.Markers) SpawnMarkerInWorld(marker);
        }

        private void SpawnMarkerInWorld(MapMarkerData markerData)
        {
            var pos = markerData.worldPosition;

            var parent = GameObject.FindWithTag("Map Path");
            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, parent.transform)
                .GetComponent<MapMarkerObject>();
            markerObj.id = markerData.id;
            markerObjects = markerObjects.Append(markerObj).ToArray();
        }

        private void DestroyMarkerInWorld(MapMarkerData marker)
        {
            var index = markerObjects.ToList().FindIndex(markerObj => markerObj.id == marker.id);
            var markerObj = markerObjects[index];
            Destroy(markerObj.gameObject);

            var list = markerObjects.ToList();
            list.RemoveAt(index);
            markerObjects = list.ToArray();
        }

        private void ClearMarkersInWorld()
        {
            foreach (var markerObj in markerObjects) Destroy(markerObj.gameObject);

            markerObjects = Array.Empty<MapMarkerObject>();
        }
    }
}