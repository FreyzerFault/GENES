using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    public class MapPath : MonoBehaviour
    {
        [SerializeField] private LineRenderer pathLineRenderer;
        [SerializeField] private LineRenderer playerToMarkerLineRenderer;

        // WORLD Markers
        [SerializeField] private GameObject marker3DPrefab;
        public MapMarkerObject[] markerObjects;

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

        private void UpdateLine(Vector3[] positions)
        {
            if (positions.Length == 0) return;

            if (startLineAtPlayer)
                UpdatePlayerLine();

            if (projectLineToTerrain)
            {
                ProjectLineToTerrain(positions);
            }
            else
            {
                pathLineRenderer.positionCount = positions.Length;
                pathLineRenderer.SetPositions(positions);
            }
        }

        private void UpdatePlayerLine()
        {
            var terrain = MapManager.Instance.TerrainData;
            var playerPos = playerTransform.position;
            var firstMarkerPos = MarkerManager.Markers[0].worldPosition;

            var segmentSamples = ProjectSegmentToTerrain(playerPos, firstMarkerPos, terrain);
            playerToMarkerLineRenderer.positionCount = segmentSamples.Length;
            playerToMarkerLineRenderer.SetPositions(segmentSamples);
        }

        private void ClearLine()
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.SetPositions(Array.Empty<Vector3>());
        }

        private void ProjectLineToTerrain(Vector3[] positions)
        {
            var lineSamples = new List<Vector3>();

            for (var i = 0; i < positions.Length - 1; i++)
            {
                Vector3 a = positions[i], b = positions[i + 1];
                lineSamples.AddRange(ProjectSegmentToTerrain(a, b, MapManager.Instance.TerrainData));
            }

            lineSamples.Add(positions[^1]);

            pathLineRenderer.positionCount = lineSamples.Count;
            pathLineRenderer.SetPositions(lineSamples.ToArray());
        }

        // Upsample un segmento proyectandolo en el terreno
        private Vector3[] ProjectSegmentToTerrain(Vector3 a, Vector3 b, TerrainData terrain)
        {
            var sampleLength = terrain.heightmapScale.x;
            var lineSamples = new List<Vector3>();
            var numSamples = Mathf.FloorToInt(Vector3.Distance(a, b) / sampleLength);
            for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                // Por cada sample, calcular su altura mapeada al terreno
                var samplePos = a + (b - a) * ((float)sampleIndex / numSamples);
                samplePos.y = terrain.GetInterpolatedHeight(samplePos) + MarkerManager.heightOffset;

                lineSamples.Add(samplePos);
            }

            return lineSamples.ToArray();
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