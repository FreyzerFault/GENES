using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    [CreateAssetMenu(fileName = "New Marker Storage", menuName = "Map/Marker Storage")]
    public class MarkerStorageSo : ScriptableObject
    {
        // Offset de altura para que clipee con el terreno
        public List<Marker> markers = new();
        public int Count => markers.Count;

        public Marker First => markers.FirstOrDefault();
        
        public Vector3[] WorldPositions => markers.Select(marker => marker.WorldPosition).ToArray();
        public Vector2[] NormalizedPositions => markers.Select(marker => marker.NormalizedPosition).ToArray();

        // ============================ HOVERED ============================
        public Marker Hovered => markers.First(marker => marker.hovered);
        public int HoveredIndex => markers.FindIndex(marker => marker.hovered);
        public bool AnyHovered => markers.Any(marker => marker.hovered);

        // ============================ SELECTED ============================
        public int SelectedCount => markers.Count(marker => marker.Selected);
        public int SelectedIndex => markers.FindIndex(marker => marker.Selected);
        public Marker Selected => SelectedCount > 0 ? markers[SelectedIndex] : null;

        // 2 Seleccionados (para crear un Marker intermedio)
        public Tuple<int, int> SelectedIndexPair =>
            SelectedCount == 0
                ? null
                : new Tuple<int, int>(SelectedIndex, markers.FindLastIndex(marker => marker.Selected));

        public Tuple<Marker, Marker> SelectedPair =>
            SelectedCount == 0
                ? null
                : new Tuple<Marker, Marker>(Selected, Get(markers.FindLastIndex(marker => marker.Selected))
                );

        #region CRUD

        public Marker Add(Vector2 normalizedPos, string label = "", int index = -1)
        {
            var marker = new Marker(normalizedPos, label: label);

            if (index < 0 || index >= Count)
                markers.Add(marker);
            else
                markers.Insert(index, marker);

            return marker;
        }

        public Marker Remove(int index)
        {
            if (index == -1 || index >= Count) return null;

            Marker markerRemoved = Get(index);
            markers.RemoveAt(index);

            return markerRemoved;
        }

        public Marker Get(int index)
        {
            if (index < 0 || index >= Count) return null;
            return markers[index];
        }

        public Marker Set(Marker marker, int index)
        {
            if (index < 0 || index >= Count) return null;
            return markers[index] = marker;
        }
        
        public void ClearAll() => markers.Clear();

        #endregion

        #region SELECTION
        
        public Marker Select(int index)
        {
            if (index < 0 || index >= Count) return null;

            var marker = markers[index];
            marker.Selected = true;
            return marker;
        }

        public Marker Deselect(int index)
        {
            if (index < 0 || index >= Count) return null;
            markers[index].Selected = false;
            return markers[index];
        }

        public void DeselectAll()
        {
            markers.ForEach(marker => marker.Selected = false);
        }

        #endregion

        public override string ToString() => string.Join("\n", markers.Select(marker => marker.ToString()));
    }
}