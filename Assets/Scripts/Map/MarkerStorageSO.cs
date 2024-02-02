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

        public Marker First => markers.FirstOrDefault();
        public Vector3[] WorldPositions => markers.Select(marker => marker.WorldPosition).ToArray();
        public Vector2[] NormalizedPositions => markers.Select(marker => marker.NormalizedPosition).ToArray();

        public int SelectedCount => markers.Count(marker => marker.IsSelected);

        // 2 Seleccionados (para crear un Marker intermedio)
        public Tuple<int, int> SelectedIndexPair =>
            new(
                markers.FindIndex(marker => marker.IsSelected),
                markers.FindLastIndex(marker => marker.IsSelected)
            );

        public int SelectedIndex => markers.FindIndex(marker => marker.IsSelected);

        public Marker Selected => SelectedCount > 0 ? markers[SelectedIndex] : null;

        public Tuple<Marker, Marker> SelectedPair =>
            SelectedCount == 0
                ? null
                : new Tuple<Marker, Marker>(
                    markers[markers.FindIndex(marker => marker.IsSelected)],
                    markers[markers.FindLastIndex(marker => marker.IsSelected)]
                );

        public int Count => markers.Count;


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

            var marker = markers[index];
            markers.RemoveAt(index);

            return marker;
        }

        public Marker Select(int index)
        {
            if (index < 0 || index >= Count) return null;

            var marker = markers[index];
            marker.Select();
            return marker;
        }

        public Marker Deselect(int index)
        {
            if (index < 0 || index >= Count) return null;
            markers[index].Deselect();
            return markers[index];
        }

        public void DeselectAll()
        {
            markers.ForEach(marker => marker.Deselect());
        }

        public Marker Set(Marker marker, int index)
        {
            if (index < 0 || index >= Count) return null;
            return markers[index] = marker;
        }

        public void ClearAll()
        {
            markers = new List<Marker>();
        }
    }
}