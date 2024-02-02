using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using PathFinding;
using UnityEngine;
#if UNITY_EDITOR
using MyBox;
#endif

namespace Map
{
    public class PathGenerator : Utils.Singleton<PathGenerator>
    {
        [SerializeField] private PathFindingConfigSO pathFindingConfig;

        public List<Path> paths;

        // TODO extraer directPath a OTRO PathGenerator
        public Path directPath;

        // Mostrar una línea directa hacia los objetivos?
        [SerializeField] private bool buildDirectPath = true;

        // PLAYER
        private static Vector3 PlayerPosition => MapManager.Instance.PlayerPosition;
        private static Vector3 PlayerDirection => MapManager.Instance.PlayerForward;

        private MarkerManager MarkerManager => MarkerManager.Instance;
        private PathFindingAlgorithm PathFinding => pathFindingConfig.Algorithm;

        private void Start()
        {
            // Min Height depends on water height
            pathFindingConfig.minHeight = MapManager.Instance.WaterHeight;

            PathFinding.CleanCache();

            // PATH
            MarkerManager.OnMarkerAdded += HandleOnMarkerAdded;
            MarkerManager.OnMarkerRemoved += HandleOnMarkerRemoved;
            MarkerManager.OnMarkerMoved += HandleOnMarkerMoved;
            MarkerManager.OnMarkersClear += ClearPaths;
            UpdatePath();
        }

        private void Update()
        {
            UpdatePlayerPath();

            // Update Direct Line Renderer
            UpdateDirectPath();
        }


        // EVENTS
        public event Action<Path, int> OnPathAdded;
        public event Action<int> OnPathDeleted;
        public event Action<Path, int> OnPathUpdated;
        public event Action<Path[]> OnAllPathsUpdated;
        public event Action OnPathsCleared;


        // ================== PATH ==================

        private void HandleOnMarkerAdded(Marker marker, int index = -1)
        {
            index = index < 0 || index >= MarkerManager.MarkerCount ? MarkerManager.MarkerCount - 1 : index;

            Vector3 mid = Vector3.zero, start = Vector3.zero, end = Vector3.zero;

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
                mid = marker.WorldPosition;
            }

            // Se coloca el primero
            // [Player ===> New Marker ===> 2º Marker -> ...]
            if (backPath)
            {
                UpdatePlayerPath();

                mid = marker.WorldPosition;
                end = MarkerManager.Markers[index + 1].WorldPosition;
            }

            // El Marker se ha insertado en medio del camino
            // [... -> i-1º Marker ===> New Marker ===> i+1º Marker -> ...]
            if (midPath)
            {
                start = MarkerManager.Markers[index - 1].WorldPosition;
                mid = marker.WorldPosition;
                end = MarkerManager.Markers[index + 1].WorldPosition;
            }


            if (start == Vector3.zero || mid == Vector3.zero) return;

            // NUEVO PATH (empuja el path[index] a path[index + 1])
            var path1 = BuildPath(start, mid);
            paths.Insert(index, path1);
            OnPathAdded?.Invoke(path1, index);


            if (end == Vector3.zero) return;

            // PATH ANTERIOR
            var path2 = BuildPath(mid, end);
            paths[index + 1] = path2;
            OnPathUpdated?.Invoke(path2, index + 1);
        }

        private void HandleOnMarkerRemoved(Marker marker, int index)
        {
            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
            {
                UpdatePlayerPath();
                paths.RemoveAt(1);
                OnPathDeleted?.Invoke(index);
                return;
            }

            // Ultimo marker eliminado, solo tiene el último path conectado
            if (index == MarkerManager.MarkerCount)
            {
                paths.RemoveAt(index);
                OnPathDeleted?.Invoke(index);
                return;
            }

            // Si el Marker es intermedio hay que fusionar sus dos paths adyacentes en uno
            // (start) Marker[i-1] =Path[i]=> deleted =Path[i+1]=> Marker[i] (end)
            // (start) Marker[i-1]          =Path[i]=>         Marker[i] (end)
            var path1 = paths[index];
            var path2 = paths[index + 1];

            paths[index] = BuildPath(
                path1.Start.Position,
                path2.End.Position,
                path1.Start.direction,
                path2.End.direction
            );
            OnPathUpdated?.Invoke(paths[index], index);

            paths.RemoveAt(index + 1);
            OnPathDeleted?.Invoke(index + 1);
        }

        private void HandleOnMarkerMoved(Marker marker, int index)
        {
            // Actualizamos sus paths conectados
            // [Marker[i-1] =Path[i]=> Marker[i] =Path[i+1]=> Marker[i+1]]

            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
                UpdatePlayerPath();
            else
                paths[index] = BuildPath(
                    MarkerManager.Markers[index - 1].WorldPosition,
                    marker.WorldPosition
                );

            OnPathUpdated?.Invoke(paths[index], index);

            if (index == paths.Count - 1) return;

            paths[index + 1] = BuildPath(
                marker.WorldPosition,
                MarkerManager.Markers[index + 1].WorldPosition
            );

            OnPathUpdated?.Invoke(paths[index + 1], index + 1);
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
                directPath = Path.EmptyPath;
                return;
            }

            if (MarkerManager.MarkerCount == 0) return;

            // Ignora los Markers Checked
            // Player -> Next -> Unchecked
            var directPathPoints = Terrain.activeTerrain.ProjectPathToTerrain(
                MarkerManager.Markers
                    .Where(marker => marker.State != MarkerState.Checked)
                    .Select(marker => marker.WorldPosition)
                    .Prepend(PlayerPosition)
                    .ToArray()
            );

            directPath = new Path(
                directPathPoints
                    .Select(point => new Node(point))
                    .ToArray()
            );
        }

        private void ClearPaths()
        {
            paths.Clear();
            directPath = Path.EmptyPath;
            OnPathsCleared?.Invoke();
        }


        // ================== PATH FINDING ==================
        private Path BuildPath(Vector3 start, Vector3 end, Vector2? initialDirection = null,
            Vector2? endDirection = null)
        {
            var startNode = new Node(start, size: pathFindingConfig.cellSize, direction: initialDirection);
            var endNode = new Node(end, size: pathFindingConfig.cellSize, direction: endDirection);
            return PathFinding.FindPath(
                startNode, endNode,
                MapManager.Terrain,
                pathFindingConfig
            );
        }

        private List<Path> BuildPath(Vector3[] checkPoints, Vector2[] initialDirections = null)
        {
            var pathsBuilt = new List<Path>();
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

#if UNITY_EDITOR
        [ButtonMethod]
        private void RedoPathFinding()
        {
            PathFinding.CleanCache();

            UpdatePath();
        }
#endif
    }
}