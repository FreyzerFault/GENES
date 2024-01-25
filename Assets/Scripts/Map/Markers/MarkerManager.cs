using System;
using System.Collections.Generic;
using System.Linq;
using MyBox;
using UnityEngine;

namespace Map.Markers
{
    public class MarkerManager : Singleton<MarkerManager>
    {
        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 0.05f;

        // UI Markers
        public GameObject markerUIPrefab;

        [SerializeField] private MarkerStorageSo markersStorage;

        public List<Marker> Markers => markersStorage.markers;
        public int MarkerCount => markersStorage.Count;
        public Marker NextMarker => Markers.First(marker => marker.IsNext);

        public Marker SelectedMarker => markersStorage.Selected;
        public int SelectedCount => markersStorage.SelectedCount;


        public event Action OnAllMarkerDeselected;
        public event Action<Marker, int> OnMarkerAdded;
        public event Action<Marker> OnMarkerDeselected;
        public event Action<Marker, int> OnMarkerMoved;
        public event Action<Marker, int> OnMarkerRemoved;
        public event Action OnMarkersClear;
        public event Action<Marker> OnMarkerSelected;

        public int FindIndex(Marker marker)
        {
            return Markers.FirstIndex(m => ReferenceEquals(m, marker));
        }

        // Buscar el Marcador dentro de un radio de colision (el mas cercano si hay varios)
        public int FindIndex(Vector2 normalizedPos)
        {
            var collisions = new List<int>();
            for (var i = 0; i < MarkerCount; i++)
                if (Markers[i].IsAtPoint(normalizedPos, pointCollisionRadius))
                    collisions.Add(i);

            // No hay => -1
            // Hay mas de 1 => El mas cercano
            return
                collisions.Count switch
                {
                    0 => -1,
                    1 => collisions[0],
                    _ => collisions.MinBy(index => Markers[index].DistanceTo(normalizedPos))
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
                SelectMarker(collisionIndex);
        }

        private Marker AddMarker(Vector2 normalizedPos, int index = -1)
        {
            if (index < 0 || index > MarkerCount) index = MarkerCount;

            var isFirst = index == 0;
            var isLast = index == MarkerCount;

            var label = index.ToString();
            var state = MarkerState.Unchecked;

            if (isLast && isFirst) // No hay marcadores
            {
                state = MarkerState.Next;
            }
            else if (isLast)
            {
                if (Markers[^1].IsChecked) state = MarkerState.Next;
            }
            else if (isFirst)
            {
                label = (int.Parse(Markers[0].LabelText) - 1).ToString();

                switch (Markers[0].State)
                {
                    case MarkerState.Next:
                        Markers[0].State = MarkerState.Unchecked;
                        state = MarkerState.Next;
                        break;
                    case MarkerState.Checked:
                        state = MarkerState.Checked;
                        break;
                }
            }
            else // Entre 2 marcadores
            {
                label = $"{Markers[index - 1].LabelText} - {Markers[index + 1].LabelText}";

                if (Markers[index - 1].IsChecked) state = MarkerState.Checked;
            }

            var marker = markersStorage.Add(normalizedPos, label, index);
            marker.State = state;

            Log("Point added: " + marker.LabelText);
            OnMarkerAdded?.Invoke(marker, -1);

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

            Log("Point added: " + marker.LabelText + " between 2 Markers");
            OnMarkerAdded?.Invoke(marker, index);

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

        private Marker SelectMarker(int index)
        {
            var marker = markersStorage.Select(index);

            if (marker == null) return null;

            // Deselect si ya hay 2 seleccionados, o si el seleccionado no es adyacente
            if (markersStorage.SelectedCount == 2 ||
                (markersStorage.SelectedCount == 1 && Math.Abs(markersStorage.SelectedIndex - index) > 1))
                markersStorage.Deselect(markersStorage.SelectedIndex);

            OnMarkerSelected?.Invoke(marker);
            Log("Point selected: " + marker.LabelText);

            return marker;
        }

        public Marker SelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindIndex(normalizedPos);

            return collisionIndex < 0 ? null : SelectMarker(collisionIndex);
        }

        private void DeselectMarker(int index)
        {
            OnMarkerDeselected?.Invoke(markersStorage.Deselect(index));
        }

        public void DeselectAllMarkers()
        {
            markersStorage.DeselectAll();
            OnAllMarkerDeselected?.Invoke();
        }

        private Marker MoveSelectedMarker(Vector2 targetPos)
        {
            var selected = markersStorage.Selected;

            // Move position
            selected.NormalizedPosition = targetPos;

            // TODO Si no funciona el mover Marker, descomentar
            // markersStorage.Set(selected, markersStorage.SelectedIndex);

            // Deselect cuando se haya movido
            selected.Deselect();

            Log("Point moved: " + selected.LabelText);
            OnMarkerMoved?.Invoke(selected, markersStorage.SelectedIndex);

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
            OnMarkersClear?.Invoke();
        }

        private static void Log(string msg)
        {
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
        }
    }
}