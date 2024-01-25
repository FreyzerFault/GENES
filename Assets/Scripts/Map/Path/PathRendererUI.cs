using System;
using System.Linq;
using ExtensionMethods;
using Map;
using PathFinding;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class PathRendererUI : MonoBehaviour
{
    [SerializeField] private UILineRenderer lineRenderer;

    private MapUIRenderer _mapUIRenderer;

    private Path _path = Path.EmptyPath;
    private Terrain _terrain;

    public Color Color
    {
        get => lineRenderer.color;
        set => lineRenderer.color = value;
    }

    private RectTransform MapRectTransform => _mapUIRenderer.GetComponent<RectTransform>();

    public Path Path
    {
        get => _path;
        set
        {
            _path = value;
            UpdateLine();
        }
    }

    public float LineThickness
    {
        get => lineRenderer.LineThickness;
        set => lineRenderer.LineThickness = value;
    }

    private void Awake()
    {
        _terrain = Terrain.activeTerrain;
        lineRenderer = GetComponent<UILineRenderer>();
        _mapUIRenderer = GetComponentInParent<MapUIRenderer>();
    }

    private void Start()
    {
        UpdateLine();
    }


    public void UpdateLine()
    {
        if (_path.NodeCount < 2)
        {
            ClearLine();
            return;
        }

        _terrain ??= Terrain.activeTerrain;

        var normPoints = _path.GetPathNormalizedPoints(_terrain.terrainData);
        var localpoints = normPoints.Select(normPoint => MapRectTransform.NormalizedToLocalPoint(normPoint));

        // Update Line Renderer
        lineRenderer.Points = localpoints.ToArray();
    }

    private void ClearLine()
    {
        lineRenderer.Points = Array.Empty<Vector2>();
    }
}