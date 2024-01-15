using System;
using System.Linq;
using EditorCools;
using ExtensionMethods;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    [CreateAssetMenu(fileName = "MapMarkerManager", menuName = "Map/MapMarkerManager")]
    public class MapMarkerManagerSO : ScriptableObject
    {
        // UI Markers
        public GameObject markerUIPrefab;
        public Color markerColor = Color.white;
        public Color selectedColor = Color.red;

        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 0.05f;

        // Offset de altura para que clipee con el terreno
        public float heightOffset = 0.5f;
        public Marker[] Markers;

        [NonSerialized] public UnityEvent<Marker> OnMarkerAdded;
        [NonSerialized] public UnityEvent<Marker> OnMarkerDeselected;
        [NonSerialized] public UnityEvent<Marker> OnMarkerMoved;
        [NonSerialized] public UnityEvent<Marker> OnMarkerRemoved;
        [NonSerialized] public UnityEvent OnMarkersClear;
        [NonSerialized] public UnityEvent<Marker> OnMarkerSelected;
        [NonSerialized] public UnityEvent<MarkerState> OnMarkerStateChanged;

        public Marker FirstMarker => Markers.FirstOrDefault();

        public int numSelectedMarkers => Markers.Count(marker => marker.selected);

        // 2 Seleccionados (para crear un Marker intermedio)
        [CanBeNull]
        private Tuple<int, int> SelectedMarkerIndexPair =>
            new(
                Markers.ToList().FindIndex(marker => marker.selected),
                Markers.ToList().FindLastIndex(marker => marker.selected)
            );

        private int SelectedMarkerIndex => Markers.ToList().FindIndex(marker => marker.selected);


        public int MarkersCount => Markers.Length;

        private void OnEnable()
        {
            Markers ??= Array.Empty<Marker>();


            // EVENTS
            OnMarkerAdded = new UnityEvent<Marker>();
            OnMarkerRemoved = new UnityEvent<Marker>();
            OnMarkersClear = new UnityEvent();
            OnMarkerSelected = new UnityEvent<Marker>();
            OnMarkerDeselected = new UnityEvent<Marker>();
            OnMarkerMoved = new UnityEvent<Marker>();
        }


        private int FindMarkerIndex(GUID id)
        {
            return Markers.ToList().FindIndex(marker => marker.id == id);
        }

        private int FindMarkerIndex(Vector2 normalizedPos)
        {
            return Markers.ToList().FindIndex(marker => marker.IsAtPoint(normalizedPos, pointCollisionRadius));
        }

        private int FindClosestMarkerIndex(Vector2 normalizedPos)
        {
            var index = -1;
            var minDistance = float.MaxValue;
            for (var i = 0; i < MarkersCount; i++)
            {
                if (!Markers[i].IsAtPoint(normalizedPos, pointCollisionRadius)) continue;
                var distance = Markers[i].DistanceTo(normalizedPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    index = i;
                }
            }

            return index;
        }

        public void AddOrSelectPoint(Vector2 normalizedPos, out Marker? marker, out bool collision)
        {
            var collisionIndex = FindClosestMarkerIndex(normalizedPos);
            collision = collisionIndex != -1;
            marker = null;

            // No hay ninguna colision => Se añade el punto
            if (collisionIndex == -1)
                switch (numSelectedMarkers)
                {
                    case 0:
                        marker = AddPoint(normalizedPos);
                        break;
                    case 1:
                        marker = MoveSelectedMarker(normalizedPos);
                        DeselectMarker();
                        break;
                    case 2:
                        marker = AddPointBetweenSelectedPair(normalizedPos);
                        DeselectMarker();
                        break;
                }
            else // COLISION => Se selecciona el punto
                marker = SelectMarker(collisionIndex);
        }

        private Marker AddPoint(Vector2 normalizedPos)
        {
            var marker = new Marker(normalizedPos);

            var list = Markers.ToList();
            list.Add(marker);
            Markers = list.ToArray();

            Log("Point added in " + marker.worldPosition);
            OnMarkerAdded.Invoke(marker);

            return marker;
        }

        private Marker? AddPointBetweenSelectedPair(Vector2 normalizedPos)
        {
            if (numSelectedMarkers != 2) return null;

            var marker = new Marker(normalizedPos);

            var list = Markers.ToList();
            list.Insert(SelectedMarkerIndexPair.Item2, marker);
            Markers = list.ToArray();

            Log("Point added in " + marker.worldPosition + " between 2 Markers");
            OnMarkerAdded.Invoke(marker);

            return marker;
        }

        public Marker? RemovePoint(Vector2 normalizedPos)
        {
            var worldPos = MapManager.Instance.TerrainData.GetWorldPosition(normalizedPos);
            var index = FindClosestMarkerIndex(normalizedPos);

            // No encuentra punto
            if (index == -1) return null;


            var marker = Markers[index];
            var list = Markers.ToList();
            list.RemoveAt(index);
            Markers = list.ToArray();

            Log("Point " + marker.labelText + " removed in " + worldPos);
            OnMarkerRemoved.Invoke(marker);

            return marker;
        }

        private Marker? SelectMarker(int index)
        {
            if (index < 0 || index >= MarkersCount) return null;

            // Deselect 1º marker if 2 are selected
            if (numSelectedMarkers == 2)
            {
                var firstIndex = SelectedMarkerIndexPair.Item1;
                if (firstIndex >= 0)
                    Markers[firstIndex].selected = false;
            }

            // SELECT
            Markers[index].selected = true;
            OnMarkerSelected.Invoke(Markers[index]);
            Log("Point selected in " + Markers[index].worldPosition);
            return Markers[index];
        }

        public Marker? SelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindClosestMarkerIndex(normalizedPos);

            if (collisionIndex < 0) return null;

            return SelectMarker(collisionIndex);
        }

        private void DeselectMarker()
        {
            var numSelected = numSelectedMarkers;
            if (numSelected == 0) return;
            if (numSelected == 1)
            {
                Markers[SelectedMarkerIndex].selected = false;
                OnMarkerDeselected.Invoke(Markers[SelectedMarkerIndex]);
            }
            else
            {
                var firstIndex = SelectedMarkerIndexPair.Item1;
                var lastIndex = SelectedMarkerIndexPair.Item2;
                Markers[firstIndex].selected = false;
                Markers[lastIndex].selected = false;
                OnMarkerDeselected.Invoke(Markers[firstIndex]);
                OnMarkerDeselected.Invoke(Markers[lastIndex]);
            }
        }

        private Marker MoveSelectedMarker(Vector2 normalizedPos)
        {
            var selectedMarkerIndex = SelectedMarkerIndex;

            var selected = Markers[selectedMarkerIndex];

            // Move position
            selected.normalizedPosition = normalizedPos;

            var worldPos = MapManager.Instance.TerrainData.GetWorldPosition(normalizedPos);
            worldPos.y += heightOffset;
            selected.worldPosition = worldPos;

            Markers[selectedMarkerIndex] = selected;

            Log("Point moved to " + selected.worldPosition);

            return Markers[selectedMarkerIndex];
        }

        public void SetState(GUID guid, MarkerState state)
        {
            Markers[FindMarkerIndex(guid)].State = state;
            OnMarkerStateChanged.Invoke(state);
        }

        [Button("Clear Markers")]
        public void ClearMarkers()
        {
            Markers = Array.Empty<Marker>();
            OnMarkersClear.Invoke();
        }

        private void Log(string msg)
        {
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
        }
    }
}