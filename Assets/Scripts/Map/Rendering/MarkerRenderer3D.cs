using System.Collections.Generic;
using UnityEngine;

namespace Map.Rendering
{
    public class MarkerRenderer3D : MonoBehaviour
    {
        [SerializeField] private GameObject marker3DPrefab;
        public List<MarkerObject> markerObjects = new();

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
            var pos = marker.WorldPosition;

            var markerObj = Instantiate(marker3DPrefab, pos, Quaternion.identity, transform)
                .GetComponent<MarkerObject>();
            markerObj.Marker = marker;

            if (index == -1)
                markerObjects.Add(markerObj);
            else
                markerObjects.Insert(index, markerObj);
        }

        private void DestroyMarkerInWorld(Marker marker, int index)
        {
            var markerObj = markerObjects[index];
            if (Application.isPlaying)
                Destroy(markerObj.gameObject);
            else
                DestroyImmediate(markerObj.gameObject);

            markerObjects.RemoveAt(index);
        }

        private void ClearMarkersInWorld()
        {
            foreach (var markerObj in markerObjects)
                if (Application.isPlaying)
                    Destroy(markerObj.gameObject);
                else
                    DestroyImmediate(markerObj.gameObject);

            markerObjects = new List<MarkerObject>();
        }
    }
}