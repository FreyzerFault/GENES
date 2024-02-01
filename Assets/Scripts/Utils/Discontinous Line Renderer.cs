using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class DiscontinousLineRenderer : MonoBehaviour
{
    [SerializeField] private UILineRenderer lineRendererUI;
    [SerializeField] private UILineRenderer[] subLineRenderersUI;
    [SerializeField] private LineRenderer lineRenderer3D;
    [SerializeField] private LineRenderer[] subLineRenderers3D;

    [SerializeField] private bool isUi;

    [SerializeField] private float maxSegmentLength = 1f;

    private float width;

    private void Awake()
    {
        lineRendererUI = GetComponent<UILineRenderer>();
        lineRenderer3D = GetComponent<LineRenderer>();
        isUi = lineRendererUI != null && lineRenderer3D == null;
        width = isUi ? lineRendererUI.LineThickness : lineRenderer3D.widthMultiplier;
    }

    private void Start()
    {
        SampleLine();
    }

    public void SampleLine()
    {
        ClearSubLines();
        if (isUi)
        {
            lineRendererUI.LineThickness = width;
            subLineRenderersUI = SampleDiscontinousLines(lineRendererUI, maxSegmentLength);
            lineRendererUI.LineThickness = 0;
        }
        else
        {
            lineRenderer3D.widthMultiplier = width;
            subLineRenderers3D = SampleDiscontinousLines(lineRenderer3D, maxSegmentLength);
            lineRenderer3D.widthMultiplier = 0;
        }
    }

    private void ClearSubLines()
    {
        if (isUi)
            foreach (var lineRenderer in subLineRenderersUI)
                if (Application.isPlaying)
                    Destroy(lineRenderer.gameObject);
                else
                    DestroyImmediate(lineRenderer.gameObject);
        else
            foreach (var lineRenderer in subLineRenderers3D)
                if (Application.isPlaying)
                    Destroy(lineRenderer.gameObject);
                else
                    DestroyImmediate(lineRenderer.gameObject);
    }

    // ========================== Sample Line to Discontinous ==========================
    private static LineRenderer[] SampleDiscontinousLines(LineRenderer lineRenderer, float maxSegmentLength = 1f)
    {
        var subLineRenderers = new List<LineRenderer>();
        var points = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(points);
        points = UpSample(points, maxSegmentLength);

        for (var i = 0; i < points.Length; i++)
            if (i % 2 == 0)
            {
                var subLineRenderer =
                    Instantiate(new GameObject(), lineRenderer.transform).AddComponent<LineRenderer>();
                subLineRenderer.positionCount = 2;
                subLineRenderer.SetPositions(points.Take(i + 2).Skip(i).ToArray());
                subLineRenderer.widthMultiplier = lineRenderer.widthMultiplier;
                subLineRenderer.startColor = lineRenderer.startColor;
                subLineRenderer.endColor = lineRenderer.endColor;
                subLineRenderers.Add(subLineRenderer);
            }

        return subLineRenderers.ToArray();
    }

    private static UILineRenderer[] SampleDiscontinousLines(UILineRenderer lineRenderer, float maxSegmentLength = 1f)
    {
        var subLineRenderers = new List<UILineRenderer>();

        var points = lineRenderer.Points;
        points = UpSample(points, maxSegmentLength);

        for (var i = 0; i < points.Length; i++)
            if (i % 2 == 0)
            {
                var subLineRenderer =
                    Instantiate(new GameObject(), lineRenderer.transform).AddComponent<UILineRenderer>();
                subLineRenderer.Points = points.Take(i + 2).Skip(i).ToArray();
                subLineRenderer.LineThickness = lineRenderer.LineThickness;
                subLineRenderer.color = lineRenderer.color;
                subLineRenderers.Add(subLineRenderer);
            }

        return subLineRenderers.ToArray();
    }

    // ========================== UPSAMPLE Full Line ==========================
    private static Vector3[] UpSample(Vector3[] points, float segmentLength = 1f)
    {
        var upsampledPoints = new List<Vector3>();
        for (var i = 0; i < points.Length - 1; i++)
            upsampledPoints.AddRange(
                UpSample(points[i], points[i + 1], segmentLength)
                    .SkipLast(1)
            );

        return upsampledPoints.ToArray();
    }

    private static Vector2[] UpSample(Vector2[] points, float segmentLength = 1f)
    {
        var upsampledPoints = new List<Vector2>();
        for (var i = 0; i < points.Length - 1; i++)
            upsampledPoints.AddRange(
                UpSample(points[i], points[i + 1], segmentLength)
                    .SkipLast(1)
            );

        return upsampledPoints.ToArray();
    }


    // ========================== UPSAMPLE Segment A -> B ==========================
    private static Vector3[] UpSample(Vector3 a, Vector3 b, float segmentLength = 1f)
    {
        var numSegments = Mathf.CeilToInt(Vector3.Distance(a, b) / segmentLength);
        var segments = Enumerable.Range(0, numSegments)
            .Select(i => Vector3.Lerp(a, b, (float)i / numSegments))
            .ToArray();
        return segments;
    }

    private static Vector2[] UpSample(Vector2 a, Vector2 b, float segmentLength = 1f)
    {
        var numSegments = Mathf.CeilToInt(Vector2.Distance(a, b) / segmentLength);
        var segments = Enumerable.Range(0, numSegments)
            .Select(i => Vector2.Lerp(a, b, (float)i / numSegments))
            .ToArray();
        return segments;
    }
}