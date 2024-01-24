using System;
using System.Linq;
using MyBox;
using UnityEngine;
using UnityEngine.Events;

namespace Map.Markers
{
    [CreateAssetMenu(fileName = "MapMarkerManager", menuName = "Map/MapMarkerManager")]
    public class MarkerManagerSO : ScriptableObject
    {
        // UI Markers
        public GameObject markerUIPrefab;

        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 0.05f;

        // Offset de altura para que clipee con el terreno
        public Marker[] Markers;
        [NonSerialized] public UnityEvent OnAllMarkerDeselected;

        [NonSerialized] public UnityEvent<Marker, int> OnMarkerAdded;
        [NonSerialized] public UnityEvent<Marker> OnMarkerDeselected;
        [NonSerialized] public UnityEvent<Marker, int> OnMarkerMoved;
        [NonSerialized] public UnityEvent<Marker, int> OnMarkerRemoved;
        [NonSerialized] public UnityEvent OnMarkersClear;
        [NonSerialized] public UnityEvent<Marker> OnMarkerSelected;
        [NonSerialized] public UnityEvent<MarkerState> OnMarkerStateChanged;

        public Marker FirstMarker => Markers.FirstOrDefault();
        public Vector3[] MarkerWorldPositions => Markers.Select(marker => marker.WorldPosition).ToArray();
        public Vector2[] MarkerNormalizedPositions => Markers.Select(marker => marker.NormalizedPosition).ToArray();

        public int numSelectedMarkers => Markers.Count(marker => marker.IsSelected);

        // 2 Seleccionados (para crear un Marker intermedio)
        private Tuple<int, int> SelectedMarkerIndexPair =>
            new(
                Markers.ToList().FindIndex(marker => marker.IsSelected),
                Markers.ToList().FindLastIndex(marker => marker.IsSelected)
            );

        private int SelectedMarkerIndex => Markers.ToList().FindIndex(marker => marker.IsSelected);


        public int MarkersCount => Markers.Length;

        private void OnEnable()
        {
            Initialize();
        }

        public void Initialize()
        {
            Markers ??= Array.Empty<Marker>();
            // Markers.ForEach(marker => marker.Ini)

            // EVENTS
            OnMarkerAdded = new UnityEvent<Marker, int>();
            OnMarkerRemoved = new UnityEvent<Marker, int>();
            OnMarkersClear = new UnityEvent();
            OnMarkerSelected = new UnityEvent<Marker>();
            OnMarkerDeselected = new UnityEvent<Marker>();
            OnMarkerMoved = new UnityEvent<Marker, int>();
            OnAllMarkerDeselected = new UnityEvent();
        }

        private int FindMarkerIndex(Vector2 normalizedPos)
        {
            return Markers.ToList().FindIndex(marker => marker.IsAtPoint(normalizedPos, pointCollisionRadius));
        }

        public int FindClosestMarkerIndex(Vector2 normalizedPos)
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

        public Marker FindNextMarker(Marker prevMarker)
        {
            var index = Markers.FirstIndex(marker => marker.Equals(prevMarker));
            return index < MarkersCount ? Markers[index + 1] : null;
        }

        public void AddOrSelectMarker(Vector2 normalizedPos)
        {
            var collisionIndex = FindClosestMarkerIndex(normalizedPos);

            // No hay ninguna colision => Se añade el punto
            if (collisionIndex == -1)
                switch (numSelectedMarkers)
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

        private Marker AddMarker(Vector2 normalizedPos)
        {
            var marker = new Marker(normalizedPos, label: (Markers.Length + 1).ToString());
            Markers = Markers.Append(marker).ToArray();

            // 1º Marker => Next
            if (Markers.Length == 1) marker.State = MarkerState.Next;

            Log("Point added: " + marker.LabelText);
            OnMarkerAdded.Invoke(marker, -1);

            return marker;
        }

        private Marker AddMarkerBetweenSelectedPair(Vector2 normalizedPos)
        {
            if (numSelectedMarkers != 2) return null;

            var marker = new Marker(normalizedPos);

            var index = SelectedMarkerIndexPair!.Item2;

            var list = Markers.ToList();
            list.Insert(index, marker);
            Markers = list.ToArray();

            // Deselect both
            Markers[index - 1].Deselect();
            Markers[index + 1].Deselect();

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

            // Si tiene de Estado Next, el siguiente pasa a ser Next
            if (marker.State == MarkerState.Next && MarkersCount > index)
                Markers[index].State = MarkerState.Next;

            Log("Point removed: " + marker.LabelText);
            OnMarkerRemoved.Invoke(marker, index);

            return marker;
        }

        private Marker SelectMarker(int index)
        {
            if (index < 0 || index >= MarkersCount) return null;

            // Deselect si ya hay 2 seleccionados, o si el seleccionado no es adyacente
            if (numSelectedMarkers == 2 || (numSelectedMarkers == 1 && Math.Abs(SelectedMarkerIndex - index) > 1))
                Markers[SelectedMarkerIndex].Deselect();


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

        public void DeselectAllMarkers()
        {
            foreach (var marker in Markers) marker.Deselect();
            OnAllMarkerDeselected.Invoke();
        }

        private Marker MoveSelectedMarker(Vector2 targetPos)
        {
            var selectedMarkerIndex = SelectedMarkerIndex;
            var selected = Markers[selectedMarkerIndex];

            // Move position
            selected.NormalizedPosition = targetPos;

            Markers[selectedMarkerIndex] = selected;

            // Deselect cuando se haya movido
            selected.Deselect();

            Log("Point moved: " + selected.LabelText);
            OnMarkerMoved.Invoke(selected, selectedMarkerIndex);

            return Markers[selectedMarkerIndex];
        }

#if UNITY_EDITOR
        [ButtonMethod]
#endif
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