using System;
using System.Collections.Generic;
using UnityEngine;

namespace Markers.Rendering
{
    public class MarkerRenderer3D : MonoBehaviour
    {
        [SerializeField] private GameObject marker3DPrefab;
        public List<MarkerObject> markerObjects = new();
        
        public event Action<MarkerObject> OnMarkerSpawned;
        public event Action<MarkerObject> OnMarkerDestroyed;

        private void Start()
        {
            MarkerManager.Instance.OnMarkerAdded += SpawnMarkerInWorld;
            MarkerManager.Instance.OnMarkerRemoved += DestroyMarkerInWorld;
            MarkerManager.Instance.OnMarkersClear += ClearMarkersInWorld;
            
            SpawnAllMarkersInWorld();
        }

        private void OnDestroy()
        {
            MarkerManager.Instance.OnMarkerAdded -= SpawnMarkerInWorld;
            MarkerManager.Instance.OnMarkerRemoved -= DestroyMarkerInWorld;
            MarkerManager.Instance.OnMarkersClear -= ClearMarkersInWorld;
        }
        
        private void SpawnAllMarkersInWorld()
        {
            foreach (Marker marker in MarkerManager.Instance.Markers) SpawnMarkerInWorld(marker);
        }

        private void SpawnMarkerInWorld(Marker marker, int index = -1)
        {
            Vector3 pos = marker.WorldPosition;

            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, transform).GetComponent<MarkerObject>();
            markerObj.Marker = marker;

            if (index == -1) markerObjects.Add(markerObj);
            else markerObjects.Insert(index, markerObj);
            
            OnMarkerSpawned?.Invoke(markerObj);
        }

        private void DestroyMarkerInWorld(Marker marker, int index)
        {
            MarkerObject markerObj = markerObjects[index];
            if (Application.isPlaying) Destroy(markerObj.gameObject);
            else DestroyImmediate(markerObj.gameObject);

            markerObjects.RemoveAt(index);
            
            OnMarkerDestroyed?.Invoke(markerObj);
        }

        private void ClearMarkersInWorld()
        {
            foreach (MarkerObject markerObj in markerObjects)
                if (Application.isPlaying) Destroy(markerObj.gameObject);
                else DestroyImmediate(markerObj.gameObject);

            markerObjects.Clear();
        }
    }
}