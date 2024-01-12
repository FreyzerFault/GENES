using System;
using System.Collections.Generic;
using System.Linq;
using PathFinding;
using UnityEngine;

namespace Map
{
    public class MapPath : MonoBehaviour
    {
        [SerializeField] private AstarConfigSO aStarConfig;

        [SerializeField] private LineRenderer pathLineRenderer;
        [SerializeField] private LineRenderer playerToMarkerLineRenderer;

        // WORLD Markers
        [SerializeField] private GameObject marker3DPrefab;
        public MapMarkerObject[] markerObjects;

        [SerializeField] private bool useAstarAlgorithm = true;
        [SerializeField] private bool projectLineToTerrain = true;
        [SerializeField] private bool startLineAtPlayer = true;

        private Transform playerTransform;
        private MapMarkerManagerSO MarkerManager => MapManager.Instance.markerManager;

        private void Awake()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;

            pathLineRenderer = GetComponentInChildren<LineRenderer>();
            markerObjects ??= Array.Empty<MapMarkerObject>();
        }

        private void Start()
        {
            MarkerManager.OnMarkerAdded.AddListener(_ => UpdateLine());
            MarkerManager.OnMarkerAdded.AddListener(SpawnMarkerInWorld);
            MarkerManager.OnMarkerRemoved.AddListener(_ => UpdateLine());
            MarkerManager.OnMarkerRemoved.AddListener(DestroyMarkerInWorld);
            MarkerManager.OnMarkersClear.AddListener(ClearLine);
            MarkerManager.OnMarkersClear.AddListener(ClearMarkersInWorld);

            UpdateLine();

            // First Markers Spawning
            InitializeMarkersInWorld();
        }

        private void Update()
        {
            if (MarkerManager.Markers.Length > 0)
                UpdatePlayerLine();
        }

        // ================== LINE RENDERER ==================

        private void UpdateLine()
        {
            UpdateLine(MarkerManager.Markers);
        }

        private void UpdateLine(MapMarkerData[] markers)
        {
            UpdateLine(MarkerManager.Markers.Select(marker => marker.worldPosition).ToArray());
        }

        private void UpdateLine(Vector3[] markerPositions)
        {
            if (markerPositions.Length == 0) return;

            var points = Array.Empty<Vector3>();

            if (startLineAtPlayer) points = points.Concat(UpdatePlayerLine()).ToArray();

            if (projectLineToTerrain)
                points = points.Concat(ProjectLineToTerrain(markerPositions)).ToArray();

            points = OffsetPoints(points, MarkerManager.heightOffset);


            pathLineRenderer.positionCount = points.Length;
            pathLineRenderer.SetPositions(points);
        }

        private Vector3[] UpdatePlayerLine()
        {
            var terrain = MapManager.Instance.terrain;
            var playerPos = playerTransform.position;
            var firstMarkerPos = MarkerManager.Markers[0].worldPosition;

            Vector3[] points;

            if (useAstarAlgorithm) // A* Algorithm:
            {
                var playerNode = new Node(playerPos, 0, null);
                var markerNode = new Node(firstMarkerPos, 0, null);
                var path = AstarAlgorithm.FindPath(playerNode, markerNode, terrain, aStarConfig);
                points = AstarAlgorithm.GetPathWorldPoints(path);
            }
            else // Project on terrain simply:
            {
                points = ProjectSegmentToTerrain(playerPos, firstMarkerPos, terrain);
            }

            return points;
        }

        private void ClearLine()
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.SetPositions(Array.Empty<Vector3>());
        }

        private Vector3[] ProjectLineToTerrain(Vector3[] points)
        {
            var terrain = MapManager.Instance.terrain;
            var lineSamples = new List<Vector3>();

            for (var i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i], b = points[i + 1];
                if (useAstarAlgorithm) // A* Algorithm:
                {
                    var aNode = new Node(a, 0, null);
                    var bNode = new Node(b, 0, null);
                    var path = AstarAlgorithm.FindPath(aNode, bNode, terrain, aStarConfig);
                    lineSamples.AddRange(AstarAlgorithm.GetPathWorldPoints(path));
                }
                else
                {
                    lineSamples.AddRange(ProjectSegmentToTerrain(a, b, MapManager.Instance.terrain));
                }
            }

            lineSamples.Add(points[^1]);

            return lineSamples.ToArray();
        }

        // Upsample un segmento proyectandolo en el terreno
        private Vector3[] ProjectSegmentToTerrain(Vector3 a, Vector3 b, Terrain terrain)
        {
            var sampleLength = terrain.terrainData.heightmapScale.x;
            var lineSamples = new List<Vector3>();
            var numSamples = Mathf.FloorToInt(Vector3.Distance(a, b) / sampleLength);
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
            // Map points adding offset to Heigth
            return points.Select(point =>
            {
                point.y += offset;
                return point;
            }).ToArray();
        }

        // ================== WORLD MARKERS ==================

        private void InitializeMarkersInWorld()
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