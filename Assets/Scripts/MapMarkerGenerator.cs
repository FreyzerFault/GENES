using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapMarkerGenerator : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler
{
    [SerializeField] private List<MapMarker> markerPoints;
    [SerializeField] private Transform markerParent;
    [SerializeField] private bool removeMode;

    // Radio en el que se considera que se ha hecho click en un punto
    [SerializeField] private float pointCollisionRadius = 10f;

    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private MapRenderer mapRenderer;

    // Objetos que siguen al cursor
    [SerializeField] private RectTransform mouseCursorMarker;
    [SerializeField] private RectTransform mouseLabel;

    // Line Renderer para unir los markers
    [SerializeField] private LineRenderer lineRenderer;


    private void Awake()
    {
        mapRenderer ??= GetComponentInParent<MapRenderer>();
        markerPoints ??= new List<MapMarker>();
        lineRenderer ??= GetComponentInChildren<LineRenderer>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (removeMode)
        {
            RemovePoint(eventData.position);
            return;
        }

        // Añade o Selecciona un punto
        AddPoint(eventData.position, out var marker, out var position, out var index, out var collision);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (mouseLabel != null)
            mouseLabel.position = eventData.position + new Vector2(0, 40);
        if (mouseCursorMarker != null)
            mouseCursorMarker.position = eventData.position;
    }

    // -1 si no hay colisión con ninguno
    private int FindMarkerIndex(Vector2 point)
    {
        return markerPoints.FindIndex(marker =>
            Vector2.Distance(marker.transform.position, point) < pointCollisionRadius);
    }

    public void AddPoint(Vector2 point, out MapMarker marker, out Vector2 position, out int index, out bool collision)
    {
        var collisionIndex = FindMarkerIndex(point);

        // No hay ninguna colision => Se añade el punto
        if (collisionIndex == -1)
        {
            marker = Instantiate(markerPrefab, point, Quaternion.identity, markerParent).GetComponent<MapMarker>();
            markerPoints.Add(marker);
            position = point;
            index = markerPoints.Count - 1;
            collision = false;

            // Tag inicial
            marker.SetLabelText("" + index + ": " + position);

            // Colores de añadido
            marker.SetMarkerColor(Color.white);
            marker.SetLabelColor(Color.white);

            Debug.Log("Point added in " + point);

            UpdateLine();
            return;
        }


        marker = markerPoints[collisionIndex];
        position = marker.transform.position;
        index = collisionIndex;
        collision = true;

        // Colores de seleccionado
        marker.SetMarkerColor(Color.white);
        marker.SetLabelColor(Color.white);

        Debug.Log("Point selected in " + point);
    }

    public MapMarker RemovePoint(Vector2 point)
    {
        var index = FindMarkerIndex(point);
        if (index == -1) return null;

        var marker = markerPoints[index];
        Destroy(marker.gameObject);
        markerPoints.RemoveAt(index);

        Debug.Log("Point" + marker.LabelText + " removed in " + point);

        UpdateLine();

        return marker;
    }

    public void ClearAllPoints()
    {
        foreach (var marker in markerPoints) Destroy(marker.gameObject);
        markerPoints.Clear();
        lineRenderer.positionCount = 0;
        Debug.Log("All points removed");
    }

    public void SetRemoveMode(bool value)
    {
        removeMode = value;
    }

    private void UpdateLine()
    {
        var linePoints = new List<Vector3>();

        foreach (var marker in markerPoints) linePoints.Add(marker.transform.localPosition);

        lineRenderer.positionCount = linePoints.Count;
        lineRenderer.SetPositions(linePoints.ToArray());
    }
}