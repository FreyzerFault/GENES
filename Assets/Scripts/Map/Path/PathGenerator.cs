using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using ExtensionMethods;
using Map.Markers;
using PathFinding;
using UnityEngine;
#if UNITY_EDITOR
using MyBox;
#endif

namespace Map.Path
{
    public class PathGenerator : Singleton<PathGenerator>
    {
        [SerializeField] private PathFindingConfigSO pathFindingConfig;

        public List<PathFinding.Path> paths;
        public PathFinding.Path directPath;

        // TODO: El MarkerManager debería gestionar un MarkerRenderer3D que los instancie y destruya
        // WORLD Markers
        [SerializeField] private GameObject marker3DPrefab;
        public List<MarkerObject> markerObjects = new();

        // TODO: El MarkerManager deberia de gestionar el TargetGroup de la Camara Aerea
        // CAM Target Group - Controla los puntos a los que debe enfocar una cámara aérea
        [SerializeField] private CinemachineTargetGroup camTargetGroup;

        // Mostrar una línea directa hacia los objetivos?
        [SerializeField] private bool buildDirectPath = true;

        // PLAYER
        private Transform playerTransform;
        private Vector3 PlayerPosition => MapManager.Instance.terrain.Project(playerTransform.position);
        private Vector3 PlayerDirection => playerTransform.forward;

        private MarkerManager MarkerManager => MarkerManager.Instance;
        private PathFindingAlgorithm PathFinding => pathFindingConfig.Algorithm;

        private void Start()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;

            // Min Height depends on water height
            pathFindingConfig.minHeight = MapManager.Instance.WaterHeight;

            PathFinding.CleanCache();

            // PATH
            MarkerManager.OnMarkerAdded += HandleOnMarkerAdded;
            MarkerManager.OnMarkerRemoved += HandleOnMarkerRemoved;
            MarkerManager.OnMarkerMoved += HandleOnMarkerMoved;
            MarkerManager.OnMarkersClear += ClearPaths;
            UpdatePath();

            // First Marker Objects Spawning
            InitializeMarkerObjects();

            // Aerial Camera Targets
            InitializeCamTargets();
        }

        private void Update()
        {
            UpdatePlayerPath();

            // Update Direct Line Renderer
            UpdateDirectPath();
        }


        // EVENTS
        public event Action<PathFinding.Path, int> OnPathAdded;
        public event Action<int> OnPathDeleted;
        public event Action<PathFinding.Path, int> OnPathUpdated;
        public event Action<PathFinding.Path[]> OnAllPathsUpdated;
        public event Action OnPathsCleared;


        // ================== PATH ==================

        private void HandleOnMarkerAdded(Marker marker, int index = -1)
        {
            index = index < 0 || index >= MarkerManager.MarkerCount ? MarkerManager.MarkerCount - 1 : index;

            Vector3 prev = Vector3.zero, start = Vector3.zero, end = Vector3.zero;

            var frontPath = index == paths.Count;
            var backPath = index == 0;
            var midPath = !frontPath && !backPath;

            // Es el 1º Marker añadido => [New Marker]
            if (frontPath && backPath)
            {
                UpdatePlayerPath();
                return;
            }

            // Se coloca el último => [... -> Nº Marker ===> New Marker]
            if (frontPath)
            {
                start = MarkerManager.Markers[index - 1].WorldPosition;
                end = marker.WorldPosition;
            }

            // Se coloca el primero
            // [Player ===> New Marker ===> 2º Marker -> ...]
            if (backPath)
            {
                UpdatePlayerPath();

                start = marker.WorldPosition;
                end = MarkerManager.Markers[index + 1].WorldPosition;
            }

            // El Marker se ha insertado en medio del camino
            // [... -> i-1º Marker ===> New Marker ===> i+1º Marker -> ...]
            if (midPath)
            {
                prev = MarkerManager.Markers[index - 1].WorldPosition;
                start = marker.WorldPosition;
                end = MarkerManager.Markers[index + 1].WorldPosition;
            }


            if (start == Vector3.zero || end == Vector3.zero) return;

            // NUEVO PATH (empuja el path[index] a path[index + 1])
            var newPath = BuildPath(start, end);
            paths.Insert(index, newPath);
            OnPathAdded?.Invoke(newPath, index);


            if (prev == Vector3.zero) return;

            // PATH ANTERIOR
            var prevPath = BuildPath(prev, start);
            paths[index - 1] = prevPath;
            OnPathUpdated?.Invoke(prevPath, index - 1);
        }

        private void HandleOnMarkerRemoved(Marker marker, int index)
        {
            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
                UpdatePlayerPath();

            UpdatePath();
        }

        private void HandleOnMarkerMoved(Marker marker, int index)
        {
            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
                UpdatePlayerPath();

            UpdatePath();
        }


        private void UpdatePath()
        {
            // Player -> Marker 1 -> Marker 2 -> ... -> Marker N
            var checkpoints = MarkerManager.Markers
                .Select(marker => marker.WorldPosition)
                .Prepend(PlayerPosition)
                .ToArray();

            paths = BuildPath(
                checkpoints,
                new[] { new Vector2(PlayerDirection.x, PlayerDirection.z) }
            );

            OnAllPathsUpdated?.Invoke(paths.ToArray());
        }

        private void UpdatePlayerPath()
        {
            if (MarkerManager.MarkerCount == 0) return;

            var path = BuildPath(
                PlayerPosition, MarkerManager.Markers[0].WorldPosition,
                new Vector2(PlayerDirection.x, PlayerDirection.z)
            );

            if (paths.Count == 0)
            {
                paths.Add(path);
                OnPathAdded?.Invoke(path, 0);
            }
            else
            {
                paths[0] = path;
                OnPathUpdated?.Invoke(path, 0);
            }
        }

        // TODO Crear otro PathGenerator para el DirectPath
        // Direct Path to every marker
        private void UpdateDirectPath()
        {
            if (!buildDirectPath)
            {
                directPath = global::PathFinding.Path.EmptyPath;
                return;
            }

            if (MarkerManager.MarkerCount == 0) return;

            // Ignora los Markers Checked
            // Player -> Next -> Unchecked
            var directPathPoints = Terrain.activeTerrain.ProjectPathToTerrain(
                MarkerManager.Markers
                    .Where(marker => marker.State != MarkerState.Checked)
                    .Select(marker => marker.WorldPosition)
                    .Prepend(playerTransform.position)
                    .ToArray()
            );

            directPath = new PathFinding.Path(
                directPathPoints
                    .Select(point => new Node(point))
                    .ToArray()
            );

            // TODO Pasar el Direct a otro PathGenerator para que haya otros renderers suscritos a sus eventos
            // OnPathUpdated?.Invoke(directPath, 0);
        }

        private void ClearPaths()
        {
            paths.Clear();
            directPath = global::PathFinding.Path.EmptyPath;
            OnPathsCleared?.Invoke();
        }


        // ================== PATH FINDING ==================
        private PathFinding.Path BuildPath(Vector3 start, Vector3 end, Vector2? initialDirection = null,
            Vector2? endDirection = null)
        {
            var startNode = new Node(start, size: pathFindingConfig.cellSize, direction: initialDirection);
            var endNode = new Node(end, size: pathFindingConfig.cellSize, direction: endDirection);
            return PathFinding.FindPath(
                startNode, endNode,
                MapManager.Instance.terrain,
                pathFindingConfig
            );
        }

        private List<PathFinding.Path> BuildPath(Vector3[] checkPoints, Vector2[] initialDirections = null)
        {
            var pathsBuilt = new List<PathFinding.Path>();
            var haveDirections = initialDirections is { Length: > 0 };

            for (var i = 1; i < checkPoints.Length; i++)
            {
                var start = checkPoints[i - 1];
                var end = checkPoints[i];

                var startDirection = haveDirections && initialDirections.Length > i - 1
                    ? initialDirections[i - 1]
                    : Vector2.zero;
                var endDirection = haveDirections && initialDirections.Length > i
                    ? initialDirections[i]
                    : Vector2.zero;

                pathsBuilt.Add(BuildPath(start, end, startDirection, endDirection));
            }

            return pathsBuilt;
        }

        // ================== WORLD MARKERS ==================

        private void InitializeMarkerObjects()
        {
            foreach (var marker in MarkerManager.Markers) SpawnMarkerInWorld(marker);

            MarkerManager.OnMarkerAdded += SpawnMarkerInWorld;
            MarkerManager.OnMarkerRemoved += DestroyMarkerInWorld;
            MarkerManager.OnMarkersClear += ClearMarkersInWorld;
        }

        private void SpawnMarkerInWorld(Marker marker, int index = -1)
        {
            var pos = marker.WorldPosition;

            var parent = GameObject.FindWithTag("Map Path");
            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, parent.transform)
                .GetComponent<MarkerObject>();
            markerObj.Data = marker;

            if (index == -1)
                markerObjects.Add(markerObj);
            else
                markerObjects.Insert(index, markerObj);
        }

        private void DestroyMarkerInWorld(Marker marker, int index)
        {
            var markerObj = markerObjects[index];
            if (Application.isPlaying)
                Destroy(markerObj.gameObject);
            else
                DestroyImmediate(markerObj.gameObject);

            markerObjects.RemoveAt(index);
        }

        private void ClearMarkersInWorld()
        {
            foreach (var markerObj in markerObjects)
                if (Application.isPlaying)
                    Destroy(markerObj.gameObject);
                else
                    DestroyImmediate(markerObj.gameObject);

            markerObjects = new List<MarkerObject>();
        }

        // ================== CAM TARGETS ==================
        private void InitializeCamTargets()
        {
            ClearCamTargets();
            UpdateCamTargets();

            MarkerManager.OnMarkerAdded += (_, _) => UpdateCamTargets();
            MarkerManager.OnMarkerRemoved += (_, _) => UpdateCamTargets();
            MarkerManager.OnMarkersClear += ClearCamTargets;
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

            UpdatePath();
            UpdatePlayerPath();
        }
#endif
    }
}