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

        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 0.05f;

        // Offset de altura para que clipee con el terreno
        public float heightOffset = 0.5f;
        public Marker[] Markers;

        [NonSerialized] public UnityEvent<Marker, int> OnMarkerAdded;
        [NonSerialized] public UnityEvent<Marker, int> OnMarkerRemoved;
        [NonSerialized] public UnityEvent<Marker, int> OnMarkerMoved;
        [NonSerialized] public UnityEvent OnMarkersClear;
        [NonSerialized] public UnityEvent<Marker> OnMarkerSelected;
        [NonSerialized] public UnityEvent<Marker> OnMarkerDeselected;
        [NonSerialized] public UnityEvent OnAllMarkerDeselected;
        [NonSerialized] public UnityEvent<MarkerState> OnMarkerStateChanged;

        public Marker FirstMarker => Markers.FirstOrDefault();

        public int numSelectedMarkers => Markers.Count(marker => marker.IsSelected);

        // 2 Seleccionados (para crear un Marker intermedio)
        [CanBeNull]
        private Tuple<int, int> SelectedMarkerIndexPair =>
            new(
                Markers.ToList().FindIndex(marker => marker.IsSelected),
                Markers.ToList().FindLastIndex(marker => marker.IsSelected)
            );

        private int SelectedMarkerIndex => Markers.ToList().FindIndex(marker => marker.IsSelected);


        public int MarkersCount => Markers.Length;

        private void OnEnable() => Initialize();

        private void Initialize()
        {
            Markers ??= Array.Empty<Marker>();

            // EVENTS
            OnMarkerAdded = new UnityEvent<Marker, int>();
            OnMarkerRemoved = new UnityEvent<Marker, int>();
            OnMarkersClear = new UnityEvent();
            OnMarkerSelected = new UnityEvent<Marker>();
            OnMarkerDeselected = new UnityEvent<Marker>();
            OnMarkerMoved = new UnityEvent<Marker, int>();
            OnAllMarkerDeselected = new UnityEvent();
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

        public void AddOrSelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindClosestMarkerIndex(normalizedPos);

            // No hay ninguna colision => Se añade el punto
            if (collisionIndex == -1)
                switch (numSelectedMarkers)
                {
                    case 0:
                        AddPoint(normalizedPos);
                        break;
                    case 1:
                        MoveSelectedMarker(normalizedPos);
                        DeselectMarkers();
                        break;
                    case 2:
                        AddPointBetweenSelectedPair(normalizedPos);
                        DeselectMarkers();
                        break;
                }
            else // COLISION => Se selecciona el punto
                SelectMarker(collisionIndex);
        }

        private Marker AddPoint(Vector2 normalizedPos)
        {
            Marker marker = new Marker(normalizedPos);
            Markers = Markers.Append(marker).ToArray();

            Log("Point added: " + marker.LabelText);
            OnMarkerAdded.Invoke(marker, -1);

            return marker;
        }

        private Marker AddPointBetweenSelectedPair(Vector2 normalizedPos)
        {
            if (numSelectedMarkers != 2) return null;

            var marker = new Marker(normalizedPos);

            int index = SelectedMarkerIndexPair!.Item2;
            
            var list = Markers.ToList();
            list.Insert(index, marker);
            Markers = list.ToArray();

            Log("Point added: " + marker.LabelText + " between 2 Markers");
            OnMarkerAdded.Invoke(marker, index);

            return marker;
        }

        public Marker RemoveMarker(Vector2 normalizedPos)
        {
            var index = FindClosestMarkerIndex(normalizedPos);

            // No encuentra punto
            if (index == -1) return null;

            var marker = Markers[index];
            var list = Markers.ToList();
            list.RemoveAt(index);
            Markers = list.ToArray();

            Log("Point removed: " + marker.LabelText);
            OnMarkerRemoved.Invoke(marker, index);

            return marker;
        }

        private Marker SelectMarker(int index)
        {
            if (index < 0 || index >= MarkersCount) return null;

            // Deselect 1º marker if 2 are selected
            if (numSelectedMarkers == 2) Markers[SelectedMarkerIndex].Deselect();

            // SELECT
            Markers[index].Select();
            
            OnMarkerSelected.Invoke(Markers[index]);
            Log("Point selected: " + Markers[index].LabelText);
            
            return Markers[index];
        }

        public Marker SelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindClosestMarkerIndex(normalizedPos);

            return collisionIndex < 0 ? null : SelectMarker(collisionIndex);
        }
        
        private void DeselectMarker(int index)
        {
            Markers[index].Deselect();
            OnMarkerDeselected.Invoke(Markers[index]);
        }
        
        private void DeselectMarkers()
        {
            foreach (Marker marker in Markers) marker.Deselect();
            OnAllMarkerDeselected.Invoke();
        }

        private Marker MoveSelectedMarker(Vector2 targetPos)
        {
            var selectedMarkerIndex = SelectedMarkerIndex;
            var selected = Markers[selectedMarkerIndex];

            // Move position
            selected.NormalizedPosition = targetPos;

            Markers[selectedMarkerIndex] = selected;

            Log("Point moved: " + selected.LabelText);
            OnMarkerMoved.Invoke(selected, selectedMarkerIndex);

            return Markers[selectedMarkerIndex];
        }

        [Button("Clear Markers")]
        public void ClearMarkers()
        {
            Markers = Array.Empty<Marker>();
            OnMarkersClear.Invoke();
        }

        private static void Log(string msg)
        {
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
        }
    }
}