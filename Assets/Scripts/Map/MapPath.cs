using System;
using System.Linq;
using Map;
using UnityEngine;

public class MapPath : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private MapMarkerManagerSO markerManager;

    private void Awake()
    {
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    private void Start()
    {
        UpdateLine();
        markerManager.OnMarkerAdded.AddListener(_ => UpdateLine());
        markerManager.OnMarkerRemoved.AddListener(_ => UpdateLine());
        markerManager.OnMarkersClear.AddListener(ClearLine);
    }

    public void UpdateLine()
    {
        UpdateLine(markerManager.Markers);
    }

    public void UpdateLine(MapMarkerData[] markers)
    {
        UpdateLine(markerManager.Markers.Select(marker => marker.worldPosition).ToArray());
    }

    public void UpdateLine(Vector3[] positions)
    {
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
    }

    public void ClearLine()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.SetPositions(Array.Empty<Vector3>());
    }
}