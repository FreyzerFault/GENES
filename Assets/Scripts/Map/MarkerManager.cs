using System;
using System.Collections.Generic;
using System.Linq;
using MyBox;
using UnityEngine;

namespace Map
{
    [Serializable]
    public enum MarkerMode
    {
        Add,
        Remove,
        Select,
        None
    }

    public class MarkerManager : Utils.Singleton<MarkerManager>
    {
        // UI Markers
        public GameObject markerUIPrefab;

        [SerializeField] private MarkerStorageSo markersStorage;

        [SerializeField] private MarkerMode markerMode = MarkerMode.None;

        private int totalMarkersAdded;

        public MarkerMode MarkerMode
        {
            get => markerMode;
            set
            {
                markerMode = value;
                OnMarkerModeChanged?.Invoke(value);
            }
        }

        public List<Marker> Markers => markersStorage.markers;
        public int MarkerCount => markersStorage.Count;
        public Marker NextMarker => Markers.First(marker => marker.IsNext);

        public Marker SelectedMarker => markersStorage.Selected;
        public int SelectedCount => markersStorage.SelectedCount;

        private void Update()
        {
            markerMode = MarkerMode.Add;

            // SHIFT => Remove Mode
            var shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift);
            if (shiftPressed) markerMode = MarkerMode.Remove;
        }

        public event Action<MarkerMode> OnMarkerModeChanged;


        public event Action OnAllMarkerDeselected;
        public event Action<Marker, int> OnMarkerAdded;
        public event Action<Marker> OnMarkerDeselected;
        public event Action<Marker, int> OnMarkerMoved;
        public event Action<Marker, int> OnMarkerRemoved;
        public event Action OnMarkersClear;
        public event Action<Marker> OnMarkerSelected;

        public int FindIndex(Marker marker)
        {
            return Markers.FindIndex(m => ReferenceEquals(m, marker));
        }

        // Buscar el Marcador dentro de un radio de colision (el mas cercano si hay varios)
        public int FindIndex(Vector2 normalizedPos)
        {
            var collisions = new List<int>();
            for (var i = 0; i < MarkerCount; i++)
                if (Markers[i].IsAtPoint(normalizedPos))
                    collisions.Add(i);

            IComparer<int> comparer = Comparer<int>.Create((index1, index2) =>
                Markers[index1].DistanceTo(normalizedPos) <
                Markers[index2].DistanceTo(normalizedPos)
                    ? -1
                    : 1);

            // No hay => -1
            // Hay mas de 1 => El mas cercano
            return
                collisions.Count switch
                {
                    0 => -1,
                    1 => collisions[0],
                    _ => collisions.OrderBy(index => Markers[index].DistanceTo(normalizedPos)).First()
                };
        }

        public void AddOrSelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindIndex(normalizedPos);

            // No hay ninguna colision => Se añade el punto
            if (collisionIndex == -1)
                switch (markersStorage.SelectedCount)
                {
                    case 0:
                        AddMarker(normalizedPos);
                        break;
                    case 1:
                        MoveSelectedMarker(normalizedPos);
                        DeselectAllMarkers();
                        break;
                    case 2:
                        AddMarkerBetweenSelectedPair(normalizedPos);
                        DeselectAllMarkers();
                        break;
                }
            else // COLISION => Se selecciona el punto
                ToggleSelectMarker(collisionIndex);
        }

        private Marker AddMarker(Vector2 normalizedPos, int index = -1)
        {
            if (index < 0 || index > MarkerCount) index = MarkerCount;

            // Actualizacion de Estado y Label segun si va primero o ultimo
            var isFirst = index == 0;
            var isLast = index == MarkerCount;

            var label = totalMarkersAdded.ToString();

            // El estado depende de la posicion relativa a otros markers y el estado de sus adyacentes
            var state = isLast && isFirst
                // 1º Marker added => NExt
                ? MarkerState.Next
                : isLast && Markers[^1].IsChecked
                    // Last Marker & todos estan checked => Next
                    ? MarkerState.Next
                    : isFirst
                        // 1º Marker => Hereda el estado del siguiente
                        ? Markers[0].State
                        : Markers[index - 1].IsChecked
                            // Añadido entre 2 markers
                            ? MarkerState.Checked
                            : MarkerState.Unchecked;

            var marker = markersStorage.Add(normalizedPos, label, index);
            marker.State = state;

            totalMarkersAdded++;

            Log("Point added: " + marker.LabelText);
            OnMarkerAdded?.Invoke(marker, index);

            return marker;
        }

        private Marker AddMarkerBetweenSelectedPair(Vector2 normalizedPos)
        {
            if (markersStorage.SelectedCount != 2) return null;

            var index = markersStorage.SelectedIndexPair!.Item2;
            var marker = AddMarker(normalizedPos, index);

            // Deselect both
            Markers[index - 1].Deselect();
            Markers[index + 1].Deselect();

            return marker;
        }

        public Marker RemoveMarker(Vector2 normalizedPos)
        {
            var index = FindIndex(normalizedPos);

            var marker = markersStorage.Remove(index);

            if (marker == null) return null;

            // Si tiene de Estado Next, el siguiente pasa a ser Next
            if (marker.State == MarkerState.Next && markersStorage.Count > index)
                Markers[index].State = MarkerState.Next;

            Log("Point removed: " + marker.LabelText);
            OnMarkerRemoved?.Invoke(marker, index);

            return marker;
        }

        private Marker ToggleSelectMarker(int index)
        {
            if (index < 0 || index >= MarkerCount) return null;

            var isSelected = markersStorage.markers[index].IsSelected;
            var twoSelected = markersStorage.SelectedCount == 2;
            var notAdyacentToSelected = markersStorage.SelectedCount == 1 &&
                                        Math.Abs(markersStorage.SelectedIndex - index) > 1;

            // Deselect si ya habia 2 seleccionados, o si el seleccionado no es adyacente
            if (!isSelected)
            {
                if (twoSelected) DeselectAllMarkers();
                else if (notAdyacentToSelected) DeselectMarker(markersStorage.Selected);
            }

            var marker = markersStorage.markers[index];
            ToggleSelectMarker(marker);
            return marker;
        }

        public Marker ToggleSelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindIndex(normalizedPos);

            return collisionIndex < 0 ? null : ToggleSelectMarker(collisionIndex);
        }

        private void ToggleSelectMarker(Marker marker)
        {
            if (marker.IsSelected)
                DeselectMarker(marker);
            else
                SelectMarker(marker);
        }

        private void SelectMarker(Marker marker)
        {
            marker.Select();
            OnMarkerSelected?.Invoke(marker);
            Log("Point selected: " + marker.LabelText);
        }

        private void DeselectMarker(Marker marker)
        {
            marker.Deselect();
            OnMarkerDeselected?.Invoke(marker);
            Log("Point deselected: " + marker.LabelText);
        }

        public void DeselectAllMarkers()
        {
            markersStorage.DeselectAll();
            OnAllMarkerDeselected?.Invoke();
        }

        private Marker MoveSelectedMarker(Vector2 targetPos)
        {
            var selectedIndex = markersStorage.SelectedIndex;
            var selected = markersStorage.markers[selectedIndex];

            // Move position
            selected.NormalizedPosition = targetPos;

            // Deselect cuando se haya movido
            selected.Deselect();

            Log("Point moved: " + selected.LabelText);
            OnMarkerMoved?.Invoke(selected, selectedIndex);

            return selected;
        }

        public void CheckMarker(int index)
        {
            if (index < 0 || index >= Markers.Count) return;

            var marker = Markers[index];
            marker.State = MarkerState.Checked;

            if (index >= MarkerCount - 1) return;

            // El Marker siguiente pasa a ser Next
            var nextMarker = Markers[index + 1];
            nextMarker.State = MarkerState.Next;
        }

        public void CheckMarker(Marker marker)
        {
            CheckMarker(FindIndex(marker));
        }

#if UNITY_EDITOR
        [ButtonMethod]
#endif
        public void ClearMarkers()
        {
            markersStorage.ClearAll();
            totalMarkersAdded = 0;
            OnMarkersClear?.Invoke();
        }

        private static void Log(string msg)
        {
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
        }
    }
}