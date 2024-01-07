using System;
using System.Collections.Generic;
using System.Linq;
using EditorCools;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    [CreateAssetMenu(fileName = "MapMarkerManager", menuName = "Map/MapMarkerManager")]
    public class MapMarkerManagerSO : ScriptableObject
    {
        public GameObject markerPrefab;

        public MapMarkerData[] Markers;
    
        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 0.05f;
        
        public int MarkersCount => Markers.Length;
    
        [NonSerialized] public UnityEvent<MapMarkerData> OnMarkerAdded;
        [NonSerialized] public UnityEvent<MapMarkerData> OnMarkerSelected;
        [NonSerialized] public UnityEvent<MapMarkerData> OnMarkerRemoved;
        [NonSerialized] public UnityEvent OnMarkersClear;

        private void OnEnable()
        {
            Markers ??= Array.Empty<MapMarkerData>();
            OnMarkerAdded = new UnityEvent<MapMarkerData>();
            OnMarkerSelected = new UnityEvent<MapMarkerData>();
            OnMarkerRemoved = new UnityEvent<MapMarkerData>();
            OnMarkersClear = new UnityEvent();
        }

        private int FindMarkerIndex(Vector2 normalizedPos)
        {
            return Markers.ToList().FindIndex(marker =>
                Vector2.Distance(marker.normalizedPosition, normalizedPos) < pointCollisionRadius);
        }
    
        public void AddPoint(Vector2 normalizedPos, out MapMarkerData marker,out bool collision)
        {
            var worldPos = MapManager.Instance.GetWorldPosition(normalizedPos);

            var collisionIndex = FindMarkerIndex(normalizedPos);

            if (collisionIndex == -1)
            {
                // No hay ninguna colision => Se añade el punto
                string label = "" + (MarkersCount - 1) + ": " + worldPos;
                marker = new MapMarkerData(normalizedPos, worldPos, label);
                
                List<MapMarkerData> list = Markers.ToList();
                list.Add(marker);
                Markers = list.ToArray();
                
                collision = false;

                Log("Point added in " + worldPos);
                OnMarkerAdded.Invoke(marker);

                return;
            }


            // COLISION => Se selecciona el punto
            marker = Markers[collisionIndex];
            collision = true;

            Log("Point selected in " + worldPos);
            OnMarkerSelected.Invoke(marker);
        }
    
        public MapMarkerData? RemovePoint(Vector2 normalizedPos)
        {
            var worldPos = MapManager.Instance.GetWorldPosition(normalizedPos);
            var index = FindMarkerIndex(normalizedPos);

            // No encuentra punto
            if (index == -1) return null;

            var marker = Markers[index];
            List<MapMarkerData> list = Markers.ToList();
            list.RemoveAt(index);
            Markers = list.ToArray();

            Log("Point " + marker.labelText + " removed in " + worldPos);
            OnMarkerRemoved.Invoke(marker);

            return marker;
        }

        [Button("Clear Markers")]
        public void ClearMarkers()
        {
            Markers = Array.Empty<MapMarkerData>();
            OnMarkersClear.Invoke();
        }
        
        private void Log(string msg) => Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
    }
}
