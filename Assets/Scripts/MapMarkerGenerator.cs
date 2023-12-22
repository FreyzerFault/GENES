using System.Collections.Generic;
using UnityEngine;

public class MapMarkerGenerator : MonoBehaviour
{
    [SerializeField] private HeightMap map;

    // Máximo Radio en el que se considera que dos marcadores colisionan
    [SerializeField] private float pointCollisionRadius = 10f;

    public List<MapMarker> MarkerPoints;


    private void Awake()
    {
        map ??= FindObjectOfType<HeightMap>();
        MarkerPoints ??= new List<MapMarker>();
    }

    // -1 si no hay colisión con ninguno
    private int FindMarkerIndex(Vector3 pos)
    {
        return MarkerPoints.FindIndex(marker =>
            Vector3.Distance(marker.worldPosition, pos) < pointCollisionRadius);
    }

    public void AddPoint(Vector2 normalizedPos, out MapMarker marker, out Vector2 position, out int index,
        out bool collision)
    {
        var worldPos = map.GetWorldPosition(normalizedPos);

        var collisionIndex = FindMarkerIndex(worldPos);

        // No hay ninguna colision => Se añade el punto
        if (collisionIndex == -1)
        {
            index = MarkerPoints.Count - 1;
            marker = new MapMarker(normalizedPos, worldPos, "" + index + ": " + worldPos);
            MarkerPoints.Add(marker);
            position = normalizedPos;
            collision = false;

            Debug.Log("Point added in " + worldPos);

            return;
        }


        marker = MarkerPoints[collisionIndex];
        position = marker.normalizedPosition;
        index = collisionIndex;
        collision = true;

        Debug.Log("Point selected in " + worldPos);
    }

    public MapMarker RemovePoint(Vector2 normalizedPos)
    {
        var worldPos = map.GetWorldPosition(normalizedPos);
        var index = FindMarkerIndex(worldPos);

        // No encuentra punto
        if (index == -1) return null;

        var marker = MarkerPoints[index];
        MarkerPoints.RemoveAt(index);

        Debug.Log("Point " + marker.labelText + " removed in " + worldPos);

        return marker;
    }

    public void ClearAllPoints()
    {
        MarkerPoints.Clear();
        Debug.Log("All points removed");
    }
}