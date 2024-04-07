using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using DavidUtils.ExtensionMethods;
using Map.PathFinding;
using UnityEngine;

namespace Map
{
    public abstract class PathGenerator : MonoBehaviour
    {
        protected List<Path> paths = new();
        public bool generatePlayerPath = true;
        public bool singlePath;

        public Path Path => paths[0];
        public Path[] Paths => paths.ToArray();
        public Path[] StaticPaths => paths.Skip(0).ToArray();

        // PLAYER
        private static Vector3 PlayerPosition => MapManager.Instance.PlayerPosition;
        private static Vector3 PlayerPositionOnTerrain =>
            MapManager.Terrain.Project(MapManager.Instance.PlayerPosition);
        private static Vector3 PlayerDirection => MapManager.Instance.PlayerForward;

        private static MarkerManager MarkerManager => MarkerManager.Instance;
        
        protected void Start()
        {
            // EVENTS
            MarkerManager.OnMarkerAdded += HandleOnMarkerAdded;
            MarkerManager.OnMarkerRemoved += HandleOnMarkerRemoved;
            MarkerManager.OnMarkerMoved += HandleOnMarkerMoved;
            MarkerManager.OnMarkersClear += ClearPaths;
            
            GameManager.Instance.player.OnPlayerStop += _ => RedoPlayerPath();

            RedoPath();
        }

        protected virtual void OnDestroy()
        {
            MarkerManager.OnMarkerAdded -= HandleOnMarkerAdded;
            MarkerManager.OnMarkerRemoved -= HandleOnMarkerRemoved;
            MarkerManager.OnMarkerMoved -= HandleOnMarkerMoved;
            MarkerManager.OnMarkersClear -= ClearPaths;
        }

        private void Update()
        {
            Debug.Log("A");
        }

        #region CRUD

        public event Action<Path, int> OnPathAdded;
        public event Action<int> OnPathDeleted;
        public event Action<Path, int> OnPathUpdated;
        public event Action<Path[]> OnAllPathsUpdated;
        public event Action OnPathsCleared;

        private void AddPath(Path path, int index = -1)
        {
            if (index == -1) index = paths.Count;
            paths.Insert(index, path);
            OnPathAdded?.Invoke(path, index);
        }

        public Path GetPath(int index) => 
            index >= paths.Count || index < 0 ? null : paths[index];

        private void SetPath(int index, Path path)
        {
            paths[index] = path;
            OnPathUpdated?.Invoke(path, index);
        }

        private void DeletePath(int index)
        {
            paths.RemoveAt(index);
            OnPathDeleted?.Invoke(index);
        }
        
        private void ClearPaths()
        {
            paths.Clear();
            OnPathsCleared?.Invoke();
        }

        #endregion

        #region MARKER EVENTS

        private void HandleOnMarkerAdded(Marker marker, int index = -1)
        {
            if (singlePath)
            {
                RedoPath();
                return;
            }

            index = index < 0 || index >= MarkerManager.MarkerCount ? MarkerManager.MarkerCount - 1 : index;

            PathPos pathPos = GetMarkerRelativePos(index);

            Vector3 mid = default, start = default, end = default;
            
            switch (pathPos)
            {
                // Es el 1º Marker añadido => [New Marker]
                case PathPos.Alone:
                    RedoPlayerPath();
                    return;
                
                // Se coloca el último => [... -> Nº Marker ===> New Marker]
                case PathPos.Back:
                    start = MarkerManager.Markers[index - 1].WorldPosition;
                    mid = marker.WorldPosition;
                    break;
                
                // Se coloca el primero => [Player ===> New Marker ===> 2º Marker -> ...]
                case PathPos.Front:
                    RedoPlayerPath();
                    mid = marker.WorldPosition;
                    end = MarkerManager.Markers[index + 1].WorldPosition;
                    break;
                
                // El Marker se ha insertado en medio del camino
                // [... -> i-1º Marker ===> New Marker ===> i+1º Marker -> ...]
                case PathPos.Mid:
                    start = MarkerManager.Markers[index - 1].WorldPosition;
                    mid = marker.WorldPosition;
                    end = MarkerManager.Markers[index + 1].WorldPosition;
                    break;
            }

            // NUEVO PATH (empuja el path[index] a path[index + 1])
            if (start != Vector3.zero && mid != Vector3.zero)
                AddPath(BuildPath(start, mid), index);

            // PATH ANTERIOR
            if (mid != Vector3.zero && end != Vector3.zero)
                AddPath(BuildPath(mid, end), index + 1);
        }

        private void HandleOnMarkerRemoved(Marker marker, int index)
        {
            if (singlePath)
            {
                RedoPath();
                return;
            }

            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0)
            {
                DeletePath(0);
                if (generatePlayerPath) RedoPlayerPath();
                return;
            }

            // Ultimo marker eliminado, solo tiene el último path conectado
            if (index == MarkerManager.MarkerCount)
            {
                DeletePath(index);
                return;
            }

            // Si el Marker es intermedio hay que fusionar sus dos paths adyacentes en uno
            // (start) Marker[i-1] =Path[i]=> deleted =Path[i+1]=> Marker[i] (end)
            // (start) Marker[i-1]          =Path[i]=>         Marker[i] (end)
            SetPath(index, Path.FromPathList(new[] { paths[index], paths[index + 1] }));
            DeletePath(index + 1);
        }

        private void HandleOnMarkerMoved(Marker marker, int index)
        {
            if (singlePath)
            {
                RedoPath();
                return;
            }

            // Actualizamos sus paths conectados
            // [Marker[i-1] =Path[i]=> Marker[i] =Path[i+1]=> Marker[i+1]]

            // PREVIOUS PATH:
            
            // Si es el 1º marcador se actualiza el camino del jugador
            if (index == 0 && generatePlayerPath)
                RedoPlayerPath();
            else
                SetPath(index, BuildPath(
                    MarkerManager.Markers[index - 1].WorldPosition,
                    marker.WorldPosition
                ));

            if (index == paths.Count - 1) return;

            // NEXT PATH:
            Path path = BuildPath(marker.WorldPosition, MarkerManager.Markers[index + 1].WorldPosition);
            SetPath(index + 1, path);
        }

        #endregion

        #region PATH POSITION CLASSIFIER

        private enum PathPos {Back, Front, Mid, Alone}
        
        private PathPos GetMarkerRelativePos(int index) =>
            paths.Count == 0
                ? PathPos.Alone 
                : index == 0
                    ? PathPos.Front 
                    : index == paths.Count
                        ? PathPos.Back 
                        : PathPos.Mid;

        #endregion

        #region BUILD PATH

        protected abstract Path BuildPath(
            Vector3 start,
            Vector3 end,
            Vector2? initialDirection = null,
            Vector2? endDirection = null
        );

        protected abstract List<Path> BuildPath(
            Vector3[] checkPoints,
            Vector2[] initialDirections = null
        );
        
        protected void RedoPath()
        {
            // Player -> Marker 1 -> Marker 2 -> ... -> Marker N
            var checkpoints = MarkerManager
                .Markers.Where(marker => !marker.IsChecked)
                .Select(marker => marker.WorldPosition)
                .ToArray();

            // Add Player as 1st Checkpoint if in Legal Position
            if (generatePlayerPath && MapManager.Instance.IsLegalPos(PlayerPositionOnTerrain))
                checkpoints = checkpoints.Prepend(PlayerPositionOnTerrain).ToArray();

            // Filter Illegal Checkpoints
            checkpoints = checkpoints.Where(MapManager.Instance.IsLegalPos).ToArray();

            ClearPaths();

            paths = BuildPath(checkpoints, new[] { new Vector2(PlayerDirection.x, PlayerDirection.z) });
            
            if (singlePath) paths = new List<Path> { Path.FromPathList(paths) };
            
            OnAllPathsUpdated?.Invoke(paths.ToArray());
        }

        private void RedoPlayerPath()
        {
            if (
                !generatePlayerPath
                || MarkerManager.MarkerCount == 0
                || !MapManager.Instance.IsLegalPos(PlayerPositionOnTerrain)
            )
                return;

            if (singlePath)
            {
                RedoPath();
                return;
            }

            Path playerPath = BuildPath(
                PlayerPosition,
                MarkerManager.NextMarker.WorldPosition,
                new Vector2(PlayerDirection.x, PlayerDirection.z)
            );

            if (paths.Count == 0)
                AddPath(playerPath);
            else
                SetPath(0, playerPath);
        }
        
        #endregion
    }
}