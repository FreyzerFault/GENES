using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils;
using DavidUtils.ExtensionMethods;
using Markers;
using Procrain.Core;
using UnityEngine;

namespace PathFinding
{
	public class PathGenerator : MonoBehaviour
	{
		[SerializeField] public Terrain terrain;

		protected List<Path> paths = new();
		public bool generatePlayerPath = true;
		public bool mergeOnSinglePath;

		public Path Path => paths[0];
		public Path[] Paths => paths.ToArray();
		public Path[] PathsBetweenMarkers => paths.Skip(0).ToArray();

		// PLAYER
		private static Vector3 PlayerPosition => MapManager.Instance.player.Position;
		private static Vector3 PlayerForward => MapManager.Instance.player.Forward;
		private static Vector3 PlayerDirection2D => new Vector2(PlayerForward.x, PlayerForward.z);
		private Vector3 PlayerPositionOnTerrain => terrain.Project(MapManager.Instance.player.Position);

		// MARKERS
		private int MarkerCount => MarkerManager.Instance.MarkerCount;
		private List<Marker> Markers => MarkerManager.Instance.Markers;
		private Marker NextMarker => MarkerManager.Instance.NextMarker;

		protected void Start()
		{
			terrain = MapManager.Terrain;
			bounds = terrain.GetBounds();

			// EVENTS
			MarkerManager.Instance.OnMarkerAdded += HandleOnMarkerAdded;
			MarkerManager.Instance.OnMarkerRemoved += HandleOnMarkerRemoved;
			MarkerManager.Instance.OnMarkerMoved += HandleOnMarkerMoved;
			MarkerManager.Instance.OnMarkersClear += ClearPaths;

			GameManager.Instance.player.OnPlayerMove += _ => RedoPlayerPath();

			RedoPath();
		}

		protected virtual void OnDestroy()
		{
			MarkerManager.Instance.OnMarkerAdded -= HandleOnMarkerAdded;
			MarkerManager.Instance.OnMarkerRemoved -= HandleOnMarkerRemoved;
			MarkerManager.Instance.OnMarkerMoved -= HandleOnMarkerMoved;
			MarkerManager.Instance.OnMarkersClear -= ClearPaths;
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

		public Path GetPath(int index) => index >= paths.Count || index < 0 ? null : paths[index];

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
			if (mergeOnSinglePath)
			{
				RedoPath();
				return;
			}

			index = index < 0 || index >= MarkerCount ? MarkerCount - 1 : index;
			PathPos pathPos = GetMarkerRelativePos(index);

			Vector3 mid = default,
				start = default,
				end = default;

			switch (pathPos)
			{
				// Es el 1º Marker añadido => [New Marker]
				case PathPos.Alone:
					RedoPlayerPath();
					return;

				// Se coloca el último => [... -> Nº Marker ===> New Marker]
				case PathPos.Back:
					start = Markers[index - 1].WorldPosition;
					mid = marker.WorldPosition;
					AddPath(BuildPath(start, mid));
					break;

				// Se coloca el primero => [Player ===> New Marker ===> 2º Marker -> ...]
				case PathPos.Front:
					RedoPlayerPath();
					mid = marker.WorldPosition;
					end = Markers[1].WorldPosition;
					AddPath(BuildPath(mid, end), 1);
					break;

				// El Marker se ha insertado en medio del camino
				// [... -> i-1º Marker ===> New Marker ===> i+1º Marker -> ...]
				case PathPos.Mid:
					start = Markers[index - 1].WorldPosition;
					mid = marker.WorldPosition;
					end = Markers[index + 1].WorldPosition;
					SetPath(index, BuildPath(start, mid));
					AddPath(BuildPath(mid, end), index + 1);
					break;
			}
		}

		private void HandleOnMarkerRemoved(Marker marker, int index)
		{
			if (mergeOnSinglePath)
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
			if (index == MarkerCount)
			{
				DeletePath(index);
				return;
			}

			// Si el Marker es intermedio, generamos un nuevo path entre los marcadores adyacentes
			// (start) Marker[i-1] =Path[i]=> deleted =Path[i+1]=> Marker[i] (end)
			// (start) Marker[i-1]          =Path[i]=>         Marker[i] (end)
			// TODO: Cuando añade direccion a cada Marker, pasarla como parametros
			Marker a = Markers[index - 1], b = Markers[index];
			SetPath(index, BuildPath(a.WorldPosition, b.WorldPosition));
			DeletePath(index + 1);
		}

		private void HandleOnMarkerMoved(Marker marker, int index)
		{
			if (mergeOnSinglePath)
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
				SetPath(index, BuildPath(Markers[index - 1].WorldPosition, marker.WorldPosition));

			if (index == paths.Count - 1) return;

			// NEXT PATH:
			Path path = BuildPath(marker.WorldPosition, Markers[index + 1].WorldPosition);
			SetPath(index + 1, path);
		}

		#endregion

		#region PATH POSITION CLASSIFIER

		private enum PathPos
		{
			Back,
			Front,
			Mid,
			Alone
		}

		private PathPos GetMarkerRelativePos(int index) =>
			paths.Count == 0 ? PathPos.Alone
			: index == 0 ? PathPos.Front
			: index == paths.Count ? PathPos.Back
			: PathPos.Mid;

		#endregion

		#region BUILD PATH

		protected virtual Path BuildPath(Vector3 start, Vector3 end, Vector2? startDir = null, Vector2? endDir = null)
		{
			if (start == end) return Path.EmptyPath;
			var startNode = new Node(start, direction: startDir);
			var endNode = new Node(end, direction: endDir);
			endNode.Parent = startNode;
			return new Path(startNode, endNode);
		}

		protected virtual List<Path> BuildPath(Vector3[] checkPoints, Vector2[] checkPointsDirs = null)
		{
			var pathsBuilt = new List<Path>();

			for (var i = 1; i < checkPoints.Length; i++)
			{
				Vector3 start = checkPoints[i - 1];
				Vector3 end = checkPoints[i];

				Vector2 startDirection = default, endDirection = default;
				if (checkPointsDirs != null)
				{
					if (checkPointsDirs.Length > i - 1) startDirection = checkPointsDirs[i - 1];
					if (checkPointsDirs.Length > i) endDirection = Vector2.zero;
				}

				pathsBuilt.Add(BuildPath(start, end, startDirection, endDirection));
			}

			return pathsBuilt;
		}

		protected void RedoPath()
		{
			// Player -> Marker 1 -> Marker 2 -> ... -> Marker N
			Vector3[] checkpoints = Markers.Where(marker => !marker.IsChecked)
				.Select(marker => marker.WorldPosition)
				.ToArray();

			// Add Player as 1st Checkpoint if in Legal Position
			if (generatePlayerPath && IsLegal(PlayerPositionOnTerrain))
				checkpoints = checkpoints.Prepend(PlayerPositionOnTerrain).ToArray();

			// Filter Illegal Checkpoints
			checkpoints = checkpoints.Where(IsLegal).ToArray();

			ClearPaths();

			paths = BuildPath(checkpoints, new[] { new Vector2(PlayerForward.x, PlayerForward.z) });

			if (mergeOnSinglePath) paths = new List<Path> { Path.FromPathList(paths) };

			OnAllPathsUpdated?.Invoke(paths.ToArray());
		}

		private void RedoPlayerPath()
		{
			if (!generatePlayerPath || MarkerCount == 0 || !IsLegal(PlayerPositionOnTerrain)) return;

			if (mergeOnSinglePath)
			{
				RedoPath();
				return;
			}

			if (MarkerManager.Instance.AllChecked) return;

			Path playerPath = BuildPath(PlayerPosition, NextMarker.WorldPosition, PlayerDirection2D);

			if (paths.Count == 0) AddPath(playerPath);
			else SetPath(0, playerPath);
		}

		#endregion

		#region RESTRICTIONS

		[SerializeField] public Bounds bounds;

		public bool IsInBounds(Vector3 pos) => bounds.Contains(pos);

		public virtual bool IsLegal(Vector3 pos) => IsInBounds(pos);
		public bool IsLegal(Vector2 normPos) => IsLegal(terrain.GetWorldPosition(normPos));

		#endregion
	}
}
