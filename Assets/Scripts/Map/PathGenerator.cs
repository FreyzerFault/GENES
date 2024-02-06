using System;
using System.Collections.Generic;
using System.Linq;
using PathFinding;
using UnityEngine;

namespace Map
{
    public abstract class PathGenerator : MonoBehaviour
    {
        public List<Path> paths = new();
        public bool generatePlayerPath = true;
        public bool singlePath;

        public Path Path => generatePlayerPath || singlePath ? paths[0] : paths[1];
        public List<Path> Paths => generatePlayerPath || singlePath ? paths : paths.Skip(0).ToList();

        // PLAYER
        protected static Vector3 PlayerPosition => MapManager.Instance.PlayerPosition;
        protected static Vector3 PlayerDirection => MapManager.Instance.PlayerForward;

        protected MarkerManager MarkerManager => MarkerManager.Instance;

        protected void Start()
        {
            // EVENTS
            MarkerManager.OnMarkerAdded += HandleOnMarkerAdded;
            MarkerManager.OnMarkerRemoved += HandleOnMarkerRemoved;
            MarkerManager.OnMarkerMoved += HandleOnMarkerMoved;
            MarkerManager.OnMarkersClear += ClearPaths;

            UpdatePath();
        }

        private void Update()
        {
            if (generatePlayerPath)
                UpdatePlayerPath();
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
            if (singlePath)
            {
                UpdatePath();
                return;
            }

            index = index < 0 || index >= MarkerManager.MarkerCount ? MarkerManager.MarkerCount - 1 : index;

            Vector3 mid = Vector3.zero, start = Vector3.zero, end = Vector3.zero;

            var isBackPath = index == paths.Count;
            var isFrontPath = index == 0;
            var midPath = !isBackPath && !isFrontPath;

            // Es el 1º Marker añadido => [New Marker]
            if (isBackPath && isFrontPath)
            {
                UpdatePlayerPath();
                return;
            }

            // Se coloca el último => [... -> Nº Marker ===> New Marker]
            if (isBackPath)
            {
                start = MarkerManager.Markers[index - 1].WorldPosition;
                mid = marker.WorldPosition;
            }

            // Se coloca el primero
            // [Player ===> New Marker ===> 2º Marker -> ...]
            if (isFrontPath)
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


            if (start != Vector3.zero && mid != Vector3.zero)
            {
                // NUEVO PATH (empuja el path[index] a path[index + 1])
                var path1 = BuildPath(start, mid);
                paths.Insert(index, path1);
                OnPathAdded?.Invoke(path1, index);
            }

            if (mid != Vector3.zero && end != Vector3.zero)
            {
                // PATH ANTERIOR
                var path2 = BuildPath(mid, end);
                paths[index + 1] = path2;
                OnPathUpdated?.Invoke(path2, index + 1);
            }
        }

        private void HandleOnMarkerRemoved(Marker marker, int index)
        {
            if (singlePath)
            {
                UpdatePath();
                return;
            }

            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
            {
                paths.RemoveAt(0);

                if (generatePlayerPath)
                    UpdatePlayerPath();

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
                path1.Start.position,
                path2.End.position,
                path1.Start.direction,
                path2.End.direction
            );
            OnPathUpdated?.Invoke(paths[index], index);

            paths.RemoveAt(index + 1);
            OnPathDeleted?.Invoke(index + 1);
        }

        private void HandleOnMarkerMoved(Marker marker, int index)
        {
            if (singlePath)
            {
                UpdatePath();
                return;
            }

            // Actualizamos sus paths conectados
            // [Marker[i-1] =Path[i]=> Marker[i] =Path[i+1]=> Marker[i+1]]

            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0 && generatePlayerPath)
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


        protected void UpdatePath()
        {
            // Player -> Marker 1 -> Marker 2 -> ... -> Marker N
            var checkpoints = MarkerManager.Markers
                .Select(marker => marker.WorldPosition)
                .ToArray();

            if (generatePlayerPath) checkpoints = checkpoints.Prepend(PlayerPosition).ToArray();

            paths = BuildPath(
                checkpoints,
                new[] { new Vector2(PlayerDirection.x, PlayerDirection.z) }
            );

            if (singlePath)
            {
                var onlyOnePath = Path.EmptyPath;
                foreach (var path in paths) onlyOnePath += path;
                paths[0] = onlyOnePath;
            }

            OnAllPathsUpdated?.Invoke(paths.ToArray());
        }

        private void UpdatePlayerPath()
        {
            if (!generatePlayerPath || MarkerManager.MarkerCount == 0) return;

            if (singlePath)
            {
                UpdatePath();
                return;
            }

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

        private void ClearPaths()
        {
            paths.Clear();
            OnPathsCleared?.Invoke();
        }

        // ========================= PATH BUILDER =========================

        protected abstract Path BuildPath(
            Vector3 start, Vector3 end, Vector2? initialDirection = null, Vector2? endDirection = null
        );

        protected abstract List<Path> BuildPath(Vector3[] checkPoints, Vector2[] initialDirections = null);
    }
}