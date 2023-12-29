using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class MapMarkerManager : MonoBehaviour
    {
        public GameObject MarkerPrefab;
        
        [SerializeField] private HeightMap map;

        // Máximo Radio en el que se considera que dos marcadores colisionan
        [SerializeField] private float pointCollisionRadius = 10f;

        public List<MapMarkerData> Markers;


        private void Awake()
        {
            map ??= FindObjectOfType<HeightMap>();
            Markers ??= new List<MapMarkerData>();
        }

        // -1 si no hay colisión con ninguno
        private int FindMarkerIndex(Vector3 pos)
        {
            return Markers.FindIndex(marker =>
                Vector3.Distance(marker.worldPosition, pos) < pointCollisionRadius);
        }

        public void AddPoint(Vector2 normalizedPos, out MapMarkerData marker,out bool collision)
        {
            var worldPos = map.GetWorldPosition(normalizedPos);

            var collisionIndex = FindMarkerIndex(worldPos);

            if (collisionIndex == -1)
            {
                // No hay ninguna colision => Se añade el punto
                string label = "" + (Markers.Count - 1) + ": " + worldPos;
                marker = new MapMarkerData(normalizedPos, worldPos, label);
                Markers.Add(marker);
                collision = false;

                Log("Point added in " + worldPos);

                return;
            }


            // COLISION => Se selecciona el punto
            marker = Markers[collisionIndex];
            collision = true;

            Log("Point selected in " + worldPos);
        }

        public MapMarkerData? RemovePoint(Vector2 normalizedPos)
        {
            var worldPos = map.GetWorldPosition(normalizedPos);
            var index = FindMarkerIndex(worldPos);

            // No encuentra punto
            if (index == -1) return null;

            var marker = Markers[index];
            Markers.RemoveAt(index);

            Log("Point " + marker.labelText + " removed in " + worldPos);

            return marker;
        }

        public void ClearAllPoints()
        {
            Markers.Clear();
            Log("All points removed");
        }

        private void Log(string msg)
        {
            Debug.Log("<color=green>Map Marker Generator: </color>" + msg);
        }
    }
}