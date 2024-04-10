using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using MyBox;
using UnityEngine;

namespace Markers
{
    [Serializable]
    public enum EditMarkerMode
    {
        Add,
        Delete,
        Select,
        None
    }

    public class MarkerManager : DavidUtils.Singleton<MarkerManager>
    {
        private static bool ShiftDown => Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.LeftShift);
        private static bool ShiftUp => Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftShift);
        
        [SerializeField] private MarkerStorageSo markersStorage;

        public float collisionRadius = 0.05f;
        private int _totalMarkersAdded;

        public List<Marker> Markers => markersStorage.markers;
        public int MarkerCount => markersStorage.Count;
        public Marker NextMarker => Markers.First(marker => marker.IsNext);

        public Marker SelectedMarker => markersStorage.Selected;
        public int SelectedCount => markersStorage.SelectedCount;

        public Marker HoveredMarker => markersStorage.Hovered;
        public int HoveredMarkerIndex => markersStorage.HoveredIndex;
        public bool AnyHovered => markersStorage.AnyHovered;

        private void Start() => GameManager.Instance.onGameStateChanged.AddListener(HandleOnGameStateChanged);
        private void OnDestroy() => GameManager.Instance.onGameStateChanged.RemoveListener(HandleOnGameStateChanged);
        
        private void Update()
        {
            // SHIFT => DELETE Mode
            if (GameManager.Instance.State == GameManager.GameState.Paused)
            {
                if (ShiftDown)
                    EditMarkerMode = EditMarkerMode.Delete;
                else if (ShiftUp)
                    EditMarkerMode = prevMarkerMode;
            }
        }

        #region EDIT MODE

        public event Action<EditMarkerMode> OnMarkerModeChanged;

        [SerializeField] private EditMarkerMode editMarkerMode = EditMarkerMode.None;
        [SerializeField] private EditMarkerMode prevMarkerMode = EditMarkerMode.None;
        
        public EditMarkerMode EditMarkerMode
        {
            get => editMarkerMode;
            set
            {
                if (value == editMarkerMode) return;
                prevMarkerMode = editMarkerMode;
                editMarkerMode = value;
                OnMarkerModeChanged?.Invoke(value);
            }
        }
        
        // Permite editar si esta el Juego en Pausa
        private void HandleOnGameStateChanged(GameManager.GameState newState) =>
            EditMarkerMode = newState == GameManager.GameState.Paused ? EditMarkerMode.Add : EditMarkerMode.None;

        private void OnDeselectAll() => DeselectAllMarkers();

        #endregion

        #region CRUD

        public event Action OnAllMarkerDeselected;
        public event Action<Marker, int> OnMarkerAdded;
        public event Action<Marker> OnMarkerDeselected;
        public event Action<Marker, int> OnMarkerMoved;
        public event Action<Marker, int> OnMarkerRemoved;
        public event Action OnMarkersClear;
        public event Action<Marker> OnMarkerSelected;

        private int FindIndex(Marker marker) => Markers.FindIndex(m => ReferenceEquals(m, marker));

        // Buscar el Marcador dentro de un radio de colision (el mas cercano si hay varios)
        public int FindIndex(Vector2 normalizedPos)
        {
            var collisions = new List<int>();
            for (var i = 0; i < MarkerCount; i++)
                if (Markers[i].IsAtPoint(normalizedPos))
                    collisions.Add(i);

            // No hay => -1
            // Hay mas de 1 => El mas cercano
            return collisions.Count switch
            {
                0 => -1,
                1 => collisions[0],
                _ => collisions.OrderBy(index => Markers[index].Distance2D(normalizedPos)).First()
            };
        }

        public void AddOrSelectMarker(Vector2 normalizedPos)
        {
            if (AnyHovered)
                ToggleSelectMarker(HoveredMarkerIndex);
            else
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
        }

        public Marker AddMarker(Vector2 normalizedPos, int index = -1)
        {
            if (index < 0 || index > MarkerCount)
                index = MarkerCount;

            // Actualizacion de Estado y Label segun si va primero o ultimo
            var isFirst = index == 0;
            var isLast = index == MarkerCount;

            var label = _totalMarkersAdded.ToString();

            // El estado depende de la posicion relativa a otros markers y el estado de sus adyacentes
            var state =
                isLast && isFirst
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

            _totalMarkersAdded++;

            Log("Point added: " + marker.LabelText);
            OnMarkerAdded?.Invoke(marker, index);

            return marker;
        }

        public Marker AddMarkerBetweenSelectedPair(Vector2 normalizedPos)
        {
            if (markersStorage.SelectedCount != 2)
                return null;

            var index = markersStorage.SelectedIndexPair!.Item2;
            var marker = AddMarker(normalizedPos, index);

            // Deselect both
            Markers[index - 1].Selected = false;
            Markers[index + 1].Selected = false;

            return marker;
        }

        public Marker RemoveHoveredMarker() => RemoveMarker(HoveredMarkerIndex);

        public Marker RemoveMarker(int index)
        {
            var marker = markersStorage.Remove(index);

            if (marker == null)
                return null;

            // Si tiene de Estado Next, el siguiente pasa a ser Next
            if (marker.State == MarkerState.Next && markersStorage.Count > index)
                Markers[index].State = MarkerState.Next;

            Log("Point removed: " + marker.LabelText);
            OnMarkerRemoved?.Invoke(marker, index);

            return marker;
        }

        public void ClearAll()
        {
            markersStorage.ClearAll();
            _totalMarkersAdded = 0;
            OnMarkersClear?.Invoke();
        }

        #endregion

        #region SELECT MARKER

        public Marker ToggleSelectMarker(int index)
        {
            if (index < 0 || index >= MarkerCount)
                return null;

            var isSelected = markersStorage.markers[index].Selected;
            var twoSelected = markersStorage.SelectedCount == 2;
            var notAdyacentToSelected =
                markersStorage.SelectedCount == 1
                && Math.Abs(markersStorage.SelectedIndex - index) > 1;

            // Deselect si ya habia 2 seleccionados, o si el seleccionado no es adyacente
            if (!isSelected)
            {
                if (twoSelected)
                    DeselectAllMarkers();
                else if (notAdyacentToSelected)
                    DeselectMarker(markersStorage.Selected);
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
            if (marker.Selected)
                DeselectMarker(marker);
            else
                SelectMarker(marker);
        }

        public void ToggleSelectMarker() => ToggleSelectMarker(HoveredMarkerIndex);

        public void SelectMarker(int index)
        {
            if (index < 0 || index >= MarkerCount)
                return;

            SelectMarker(Markers[index]);
        }

        public void SelectMarker(Marker marker)
        {
            marker.Selected = true;
            OnMarkerSelected?.Invoke(marker);
            Log("Point selected: " + marker.LabelText);
        }

        public void DeselectMarker(int index)
        {
            if (index < 0 || index >= MarkerCount)
                return;

            DeselectMarker(Markers[index]);
        }

        public void DeselectMarker(Marker marker)
        {
            marker.Selected = false;
            OnMarkerDeselected?.Invoke(marker);
            Log("Point deselected: " + marker.LabelText);
        }

        public void DeselectAllMarkers()
        {
            markersStorage.DeselectAll();
            OnAllMarkerDeselected?.Invoke();
        }

        #endregion

        #region MOVE MARKER

        public Marker MoveMarker(int index, Vector2 targetPos)
        {
            if (index < 0 || index >= MarkerCount)
                return null;

            var marker = Markers[index];

            // Move position
            marker.NormalizedPosition = targetPos;

            Log("Point moved: " + marker.LabelText);
            OnMarkerMoved?.Invoke(marker, index);

            return marker;
        }

        public Marker MoveSelectedMarker(Vector2 targetPos)
        {
            var selectedIndex = markersStorage.SelectedIndex;

            var moved = MoveMarker(selectedIndex, targetPos);

            // Deselect cuando se haya movido
            moved.Selected = false;

            return moved;
        }

        #endregion

        #region CHECK MARKER

        public void CheckMarker(int index)
        {
            if (index < 0 || index >= Markers.Count)
                return;

            var marker = Markers[index];
            marker.State = MarkerState.Checked;

            if (index >= MarkerCount - 1)
                return;

            // El Marker siguiente pasa a ser Next
            var nextMarker = Markers[index + 1];
            nextMarker.State = MarkerState.Next;
        }

        public void CheckMarker(Marker marker) => CheckMarker(FindIndex(marker));

        #endregion
        

#if UNITY_EDITOR
        [ButtonMethod]
#endif
        public void ClearMarkers()
        {
            ClearAll();
        }

        private static void Log(string msg) =>
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
    }
}
